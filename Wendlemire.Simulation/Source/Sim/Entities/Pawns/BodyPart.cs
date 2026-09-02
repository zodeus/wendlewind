using System.Text.RegularExpressions;

namespace Wendlemire.Sim.Entities.Pawns;

public class BodyPart : Entity
{
    public const float SkinDamageScaler = 0.6f;
    public const double DestroyedEnterHitPoints = 0.1;

    public event Action<BodyPartModifier, BodyPartModifierEventType>? ModifiersChanged;
    public event Action<BodyPart, List<DamagedBodyPartRecord>>? PartDamaged; //todo - actions
    public event Action<BodyPart>? HealthChanged; //todo - actions

    private double _hitPoints;
    private bool _isDestroyed;
    private int _destroyedRecoverTicks;
    private string? _adaptedLabel;
    private string? _internalLabel;
    private bool _isSevered; // todo, this should be set by an applied health condition
    private SubstanceType? _substanceOverride;
    private List<BodyPart>? _externalPartsCache;
    private List<BodyPart>? _internalPartsCache;
    private List<BodyPart>? _allInternalPartsCache;

    public double MaxHitPoints;
    public bool IsCracked = false;
    public BodyPartSocket? Socket;
    public List<BodyPartSocket> Sockets = new();
    public Dictionary<EquipmentSlotType, Item?> Equipment = new();
    public List<BodyPartModifier> Modifiers = new();
    public int TicksSinceLastHit = int.MaxValue;
    public BodyPartDef BodyPartDef => (BodyPartDef)Def;

    public override string Label => _adaptedLabel ?? "failed to adapt label";
    
    /// <summary>
    /// Returns a unique identifier for this body part instance, combining the Def.Moniker with position info.
    /// Unlike Label, this is guaranteed to be unique across all parts in a body.
    /// </summary>
    public string InternalLabel => _internalLabel ??= GenerateInternalLabel();
    public BodyPartType Type => BodyPartDef.BodyPartType;
    public float BloodAmount => BodyPartDef.BloodAmount;
    public float HitWeight => BodyPartDef.HitWeight;
    public double HealthPercent => HitPoints / MaxHitPoints;
    public bool IsExternal => Socket?.IsExternal ?? true;
    public SubstanceType Substance => _substanceOverride ?? BodyPartDef.Substance;
    public bool IsOrgan => BodyPartDef.IsOrgan;
    public bool IsVital => BodyPartDef.IsVital;
    /// <summary>
    /// Sticky destroyed flag. Crossing the enter threshold (low HP) destroys the part immediately;
    /// it stays destroyed until HP holds at the recover threshold so regen vs DoT cannot strobe
    /// functional / mobility / UI color every tick.
    /// </summary>
    public new bool IsDestroyed => _isDestroyed;
    public bool IsBleeding => HealthPercent < .99 && Substance == SubstanceType.Flesh; //todo coagulation

    public List<EquipmentSlotType>? EquipmentSlots => BodyPartDef.EquipmentSlots;

    public bool HasEquipmentSlots => BodyPartDef.EquipmentSlots?.Count > 0;


    public double HitPoints
    {
        get => _hitPoints;
        set
        {
            var previous = _hitPoints;
            _hitPoints = Math.Clamp(value, 0, MaxHitPoints);
            if (_hitPoints <= DestroyedEnterHitPoints)
            {
                _isDestroyed = true;
                _destroyedRecoverTicks = 0;
            }
            else if (IsImmediateRestore(previous, _hitPoints))
            {
                _isDestroyed = false;
                _destroyedRecoverTicks = 0;
            }

            HealthChanged?.Invoke(this);
            if (Body != null)
            {
                Body.BodyPartsDirty = true;
            }
        }
    }

    private bool IsImmediateRestore(double previous, double next)
    {
        if (MaxHitPoints <= 0 || next < MaxHitPoints)
        {
            return false;
        }

        // Bandages / medkits jump a destroyed part back to full HP. A 0.1/tick climb
        // that happens to reach max must still sit in the recover hold or 1-HP parts strobe.
        return previous <= DestroyedEnterHitPoints || next - previous >= MaxHitPoints * 0.5;
    }

    #region Dynamic Getters

    public PawnBody? Body => Socket?.Body ?? Socket?.ParentPart?.Body;

    public bool IsSevered
    {
        get
        {
            if (_isSevered)
            {
                return true;
            }

            return Socket?.ParentPart?.IsSevered ?? false;
        }
        private set => _isSevered = value;
    }

    public bool IsArteryFunctional
    {
        get
        {
            if (Type == BodyPartType.Artery && IsDestroyed)
            {
                return false;
            }

            if (IsExternal && InternalParts.Any(part => part.Type == BodyPartType.Artery && part.IsDestroyed))
            {
                return false;
            }

            return Socket?.ParentPart?.IsArteryFunctional ?? true;
        }
    }

    public List<BodyPart> Bones => AllInternalParts.Where(part => part.Substance == SubstanceType.Bone).ToList();
    public List<BodyPart> Arteries => AllInternalParts.Where(part => part.Type == BodyPartType.Artery).ToList();

    public bool HasBones => Bones.Count > 0;

    public bool HasBrokenBones => Bones.Any(part => part.IsDestroyed);

    public BodyPart? Skin => InternalParts.FirstOrNull(part => part?.Type == BodyPartType.Skin);

    public bool HasMobility
    {
        get
        {
            if (IsDestroyed)
            {
                return false;
            }

            if (HasBrokenBones)
            {
                return false;
            }

            if (IsArteryFunctional == false)
            {
                return false;
            }

            if (Socket?.ParentPart is { HasMobility: false })
            {
                return false;
            }

            return true;
        }
    }

    public bool IsFunctional
    {
        get
        {
            if (IsDestroyed)
            {
                return false;
            }

            if (IsArteryFunctional == false)
            {
                return false;
            }

            if (Socket?.ParentPart is { IsFunctional: false })
            {
                return false;
            }

            return true;
        }
    }

    public float AttackSpeedModifier
    {
        get
        {
            if (HasMobility == false)
            {
                return 0;
            }

            float points = this.GetStatValue(Defs.Stats.AttackSpeedModifier);
            foreach (BodyPart bodyPart in ExternalParts)
            {
                points += bodyPart.AttackSpeedModifier;
            }

            return points;
        }
    }

    public List<BodyPart> ExternalParts
    {
        get
        {
            if (_externalPartsCache != null)
            {
                return _externalPartsCache;
            }

            _externalPartsCache = new List<BodyPart>();
            foreach (BodyPartSocket socket in Sockets)
            {
                if (socket.AttachedPart?.IsExternal == true)
                {
                    _externalPartsCache.Add(socket.AttachedPart);
                }
            }

            return _externalPartsCache;
        }
    }

    public List<BodyPart> InternalParts
    {
        get
        {
            if (_internalPartsCache != null)
            {
                return _internalPartsCache;
            }

            _internalPartsCache = new List<BodyPart>();
            foreach (BodyPartSocket socket in Sockets)
            {
                if (socket.AttachedPart?.IsExternal == false)
                {
                    _internalPartsCache.Add(socket.AttachedPart);
                }
            }

            return _internalPartsCache;
        }
    }

    public List<BodyPart> AllInternalParts
    {
        get
        {
            if (_allInternalPartsCache != null)
            {
                return _allInternalPartsCache;
            }

            _allInternalPartsCache = new List<BodyPart>();
            GetParts(this, _allInternalPartsCache, false);
            return _allInternalPartsCache;
        }
    }

    public void InvalidateStructureCaches()
    {
        _externalPartsCache = null;
        _internalPartsCache = null;
        _allInternalPartsCache = null;
    }

    public static void NotifyStructureChanged(BodyPart? part)
    {
        var current = part;
        while (current != null)
        {
            current.InvalidateStructureCaches();
            current = current.Socket?.ParentPart;
        }

        part?.Body?.InvalidatePartCaches();
    }

    public Item? Armor
    {
        get
        {
            foreach (Item? item in Equipment.Values)
            {
                if (item?.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Armor)
                {
                    return item;
                }
            }

            return null;
        }
    }

    public BodyPartPosition? Position => Socket?.Position;

    #endregion

    public override void Initialize()
    {
        base.Initialize();
        MaxHitPoints = (int)this.GetStatValue(Defs.Stats.MaxHitPoints);
        HitPoints = MaxHitPoints;

        //Register Sockets
        foreach (BodyPartSocketDef bodyPartSocketDef in BodyPartDef.Sockets)
        {
            Sockets.Add(new BodyPartSocket(bodyPartSocketDef, this));
        }

        if (BodyPartDef.EquipmentSlots != null)
        {
            foreach (EquipmentSlotType slot in BodyPartDef.EquipmentSlots)
            {
                Equipment.Add(slot, null);
            }
        }
    }

    public override void Tick()
    {
        var hpBefore = _hitPoints;
        for (int index = Modifiers.Count - 1; index >= 0; index--)
        {
            BodyPartModifier modifier = Modifiers[index];
            modifier.Tick();
            if (Body?.Pawn.IsDead == true)
            {
                NotifyTickDelta(hpBefore);
                return;
            }

            TicksSinceLastHit++;
            if (modifier.IsExpired)
            {
                RemoveModifier(modifier);
            }
        }

        base.Tick();
        UpdateDestroyedRecovery();
        NotifyTickDelta(hpBefore);
    }

    public double DestroyedRecoverHitPoints
    {
        get
        {
            if (MaxHitPoints <= DestroyedEnterHitPoints)
            {
                return MaxHitPoints;
            }

            return Math.Min(MaxHitPoints, Math.Max(1.0, MaxHitPoints * 0.2));
        }
    }

    public static int DestroyedRecoverHoldTicks => Math.Max(1, GameContext.TicksPerSecond / 4);

    private void UpdateDestroyedRecovery()
    {
        if (!_isDestroyed)
        {
            _destroyedRecoverTicks = 0;
            return;
        }

        if (_hitPoints < DestroyedRecoverHitPoints)
        {
            _destroyedRecoverTicks = 0;
            return;
        }

        _destroyedRecoverTicks++;
        if (_destroyedRecoverTicks >= DestroyedRecoverHoldTicks)
        {
            _isDestroyed = false;
            _destroyedRecoverTicks = 0;
        }
    }

    private void NotifyTickDelta(double hpBefore)
    {
        var delta = _hitPoints - hpBefore;
        if (delta != 0)
        {
            Body?.NotifyTickHealthChanged(this, delta);
        }
    }

    private void GetParts(BodyPart part, List<BodyPart> parts, bool? partIsExternal = null)
    {
        foreach (BodyPartSocket socket in part.Sockets)
        {
            if (partIsExternal != null && socket.AttachedPart?.IsExternal == partIsExternal)
            {
                parts.Add(socket.AttachedPart);
                GetParts(socket.AttachedPart, parts, partIsExternal);
            }
        }
    }

    public List<BodyPartSocket> GetSocketsFor(BodyPartType bodyPartType)
    {
        List<BodyPartSocket> sockets = new();
        foreach (BodyPartSocket bodyPartSocket in Sockets)
        {
            if (bodyPartSocket.CanSocket(bodyPartType))
            {
                sockets.Add(bodyPartSocket);
            }
        }

        return sockets;
    }

    public override string ToString()
    {
        return $"{Label} ({HitPoints:0.000})";
    }

    public double ApplyDamage(DamageContext ctx, List<DamagedBodyPartRecord> damagedParts, bool cascade = true)
    {
        TicksSinceLastHit = 0;
        var wasDestroyedBeforeDamage = IsDestroyed;
        var wasFunctional = IsFunctional;

        // Apply substance modifier from weapon properties
        var substanceModifier = ctx.GetSubstanceModifier?.Invoke(Substance) ?? 1f;

        // Depth penetration: this layer absorbs a portion, the rest penetrates to deeper structures
        // This ensures damage is conserved while still reaching internal parts
        const double surfaceAbsorptionRate = 0.5; // Each layer absorbs 50%, 50% penetrates
        var damageToAbsorb = ctx.Amount * surfaceAbsorptionRate;
        var damageToPenetrate = ctx.Amount * (1.0 - surfaceAbsorptionRate);

        var scaledDamageToAbsorb = damageToAbsorb * substanceModifier;
        var prevHP = HitPoints;
        HitPoints = Math.Max(0, HitPoints - scaledDamageToAbsorb);
        var damageApplied = prevHP - HitPoints;

        // Remaining = penetrating portion + any damage this part couldn't absorb (if destroyed)
        var unabsorbedDamage = Math.Max(0, damageToAbsorb - damageApplied / substanceModifier);
        var remainingDamage = damageToPenetrate + unabsorbedDamage;

        if (HealthPercent < .1 && Context.Rng.Chance(0.3f) && Substance == SubstanceType.Chitin)
        {
            IsCracked = true;
        }

        var wasDestroyed = wasDestroyedBeforeDamage == false && IsDestroyed;
        var stoppedFunctioning = wasFunctional && IsFunctional == false;

        var record = new DamagedBodyPartRecord(this)
        {
            DamageApplied = damageApplied,
            WasDestroyed = wasDestroyed,
            StoppedFunctioning = stoppedFunctioning
        };
        this.ApplyBodyPartModifiers(ctx.BodyPartModifiers, record, ctx.WeaponManeuver);
        damagedParts.Add(record);
        if (remainingDamage > 0 && cascade)
        {
            remainingDamage = this.CascadeDamageToInternalParts(ctx.WithAmount(remainingDamage), damagedParts);
        }

        return remainingDamage;
    }

    public List<DamagedBodyPartRecord> ApplyDamageToExternalPart(Damage damage, List<DamagedBodyPartRecord>? damagedParts = null)
    {
        damagedParts ??= [];
        var ctx = DamageContext.FromDamage(damage, damage.TotalUnblockedDamage);
        var remainingDamage = ApplyDamage(ctx, damagedParts, cascade: false);

        // Cascade damage to internal parts (skin is handled first in CascadeDamageToInternalParts)
        if (remainingDamage > 0)
        {
            this.CascadeDamageToInternalParts(ctx.WithAmount(remainingDamage), damagedParts);
        }

        this.PotentiallySevereLimb();
        damagedParts[0].WasSevered = IsSevered;
        PartDamaged?.Invoke(this, damagedParts);

        return damagedParts;
    }

    public void Severe()
    {
        if (Socket == Body?.RootSocket)
        {
            Log.Warning($"Attempted to severe part attached to root socket Part={Socket?.AttachedPart?.Label} Pawn={Socket?.Body?.Pawn.Label}");
            return;
        }

        var parentPart = Socket?.ParentPart;
        var body = Body;
        body?.Handler.OnPartSevered(this);
        if (Socket != null)
        {
            Socket.Body = null; //todo not that it matters, but this should probably also set Pawn.Body.RootSocket = null as well
            Socket.AttachedPart = null;
            Socket.IsSealed = false;
            Socket = null;
        }

        IsSevered = true;
        InvalidateStructureCaches();
        NotifyStructureChanged(parentPart);
        body?.InvalidatePartCaches();
    }

    public void TryAddModifier(BodyPartModifier modifier)
    {
        //Log.Debug($"Attempting to apply BodyPartModifier: {modifier.Label} to {this}");
        BodyPartModifier? existingModifier = Modifiers.FirstOrNull(m => m?.Def == modifier.Def);
        if (existingModifier != null)
        {
            existingModifier.MergeWith(modifier);
            return;
        }

        modifier.BodyPart = this;
        Modifiers.Add(modifier);
        ModifiersChanged?.Invoke(modifier, BodyPartModifierEventType.Added);
    }

    private void RemoveModifier(BodyPartModifier modifier)
    {
        Modifiers.Remove(modifier);
        ModifiersChanged?.Invoke(modifier, BodyPartModifierEventType.Removed);
    }

    public bool HasModifier(BodyPartModifierDef def)
    {
        return Modifiers.Any(m => m.Def == def);
    }

    /// <summary>
    /// Sets an override for the body part's substance type.
    /// </summary>
    public void SetSubstanceOverride(SubstanceType substance)
    {
        _substanceOverride = substance;
    }

    /// <summary>
    /// Clears any substance override, returning to the default from the definition.
    /// </summary>
    public void ClearSubstanceOverride()
    {
        _substanceOverride = null;
    }

    /// <summary>
    /// Returns true if the substance is currently overridden from its default.
    /// </summary>
    public bool HasSubstanceOverride => _substanceOverride.HasValue;

    #region Equipment

    public EquipmentSlotType? SlotFor(Item item)
    {
        var slot = item.ItemDef.EquipmentProperties?.SlotUsedToEquip;
        if (slot == null || EquipmentSlots == null) return null;
        return EquipmentSlots.Contains(slot.Value) ? slot.Value : null;
    }

    public EquipmentSlotType? EmptySlotFor(Item item)
    {
        if (!HasEquipmentSlots) return null;

        if (item.ItemDef.ItemType == ItemType.Potion)
        {
            var capacity = Body?.Pawn.PotionCapacity ?? PotionSlots.BaseSlots;
            foreach (EquipmentSlotType potionSlot in EquipmentSlots!.Where(PotionSlots.IsPotionSlot))
            {
                if (!PotionSlots.IsUnlocked(potionSlot, capacity))
                {
                    continue;
                }

                if (Equipment[potionSlot] == null)
                {
                    return potionSlot;
                }
            }

            return null;
        }

        var slot = item.ItemDef.EquipmentProperties?.SlotUsedToEquip;
        if (slot == null) return null;
        if (slot == EquipmentSlotType.HandWeapon
            && Body?.Pawn.Equipment.HasTwoHandedWeapon() == true)
        {
            return null;
        }

        foreach (EquipmentSlotType potentialSlot in EquipmentSlots!)
        {
            if (potentialSlot == slot && Equipment[potentialSlot] == null)
            {
                return potentialSlot;
            }
        }

        return null;
    }

    #endregion

    public Item? UnEquip(Item itemToUnEquip)
    {
        foreach ((EquipmentSlotType slot, Item? item) in Equipment)
        {
            if (Equals(item, itemToUnEquip))
            {
                Equipment[slot] = null;
                return item;
            }
        }

        return null;
    }

    public void AdaptBodyPartTo(BodyPart? parentPart)
    {
        _adaptedLabel = GenerateLabel();
        if (Body != null)
        {
            MaxHitPoints = Mathf.FloorToInt((float)(MaxHitPoints * Body.BodySizeFactor));
            HitPoints = MaxHitPoints;
        }
        else
        {
            Log.Warning("BodyPart.AdaptBodyPartTo failed to scale BodyPart, no reference to Body.");
        }

        if (BodyPartDef.AdaptiveProperties == null)
        {
            ApplyHumanHpScale();
            return;
        }

        if (parentPart == null)
        {
            Log.Error($"Attempting to adapt part '{this}' but part doesn't have parent");
            return;
        }

        MaxHitPoints = Mathf.FloorToInt(BodyPartDef.AdaptiveProperties.MaxHitPointScaler.GetMaxHitPointsFor(parentPart));
        HitPoints = MaxHitPoints;
    }

    private void ApplyHumanHpScale()
    {
        if (Body?.Pawn.Species != "Human")
        {
            return;
        }

        var scale = CombatBalance.ScaleFor(Type);
        if (scale == 1f)
        {
            return;
        }

        MaxHitPoints = Mathf.FloorToInt((float)(MaxHitPoints * scale));
        HitPoints = MaxHitPoints;
    }

    private string GenerateLabel()
    {
        string label = "";
        if (Socket?.Def.Position != null)
        {
            label += Socket?.ParentPart?.Position == null ? "" : string.Join(" ", Regex.Split(Socket?.ParentPart?.Position.ToString()!, @"(?<!^)(?=[A-Z])")) + " ";
        }

        // Skip position prefix for minions (they use InternalLabel for layout lookups)
        label += Position == null || IsMinionPosition(Position.Value) ? "" : string.Join(" ", Regex.Split(Position.ToString()!, @"(?<!^)(?=[A-Z])")) + " ";
        label += BodyPartDef.Label;
        return label;
    }
    
    private string GenerateInternalLabel()
    {
        var moniker = BodyPartDef.Moniker;
        
        // Append position for disambiguation if available
        if (Position != null)
        {
            moniker += "_" + Position.Value;
        }
        // Fallback: use socket def moniker for further disambiguation
        else if (Socket?.Def.Moniker != null)
        {
            moniker += "_" + Socket.Def.Moniker;
        }
         
        return moniker;
    }

    private bool IsMinionPosition(BodyPartPosition position)
    {
        return position is BodyPartPosition.M1 or BodyPartPosition.M2 or BodyPartPosition.M3 or BodyPartPosition.M4 or BodyPartPosition.M5 or BodyPartPosition.M6 or BodyPartPosition.M7 or BodyPartPosition.M8 or BodyPartPosition.M9 or BodyPartPosition.M10;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _hitPoints, "HitPoints");
        ScribeValues.Look(ref _isDestroyed, "IsDestroyedLatched");
        if (_hitPoints <= DestroyedEnterHitPoints)
        {
            _isDestroyed = true;
        }
        else if (MaxHitPoints > 0 && _hitPoints >= MaxHitPoints)
        {
            _isDestroyed = false;
        }

        ScribeValues.Look(ref _adaptedLabel!, "AdaptedLabel");
        ScribeValues.Look(ref _isSevered, "IsCracked");
        ScribeValues.Look(ref _isSevered, "IsSevered");
        ScribeValues.Look(ref _substanceOverride, "SubstanceOverride");
        ScribeValues.Look(ref MaxHitPoints, "MaxHitPoints");
        ScribeValues.Look(ref TicksSinceLastHit, "TicksSinceLastHit");
        ScribeReferences.Look(ref Socket!, "Socket");
        ScribeCollections.Look(ref Sockets!, "Sockets", LookMode.Deep);
        ScribeCollections.Look(ref Modifiers!, "Modifiers", LookMode.Deep);
        ScribeCollections.Look(ref Equipment!, "Equipment", LookMode.Value, LookMode.Deep);
        InvalidateStructureCaches();
    }
}