using System.Text.RegularExpressions;
using Grafted.Graphics.Textures;
using SharpDX;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPart : Entity
{
    private const float SKIN_DAMAGE_SCALER = 0.6f;
    public event Action<BodyPart, List<DamagedBodyPartRecord>>? PartDamaged; //todo - actions

    private float _hitPoints;
    private string? _adaptedLabel;
    private bool _isSevered; // todo, this should be set by an applied health condition
    private Texture2D? _image;

    public float MaxHitPoints;
    public BodyPartSocket? Socket;
    public List<BodyPartSocket> Sockets = new();
    public Dictionary<EquipmentSlotType, Item?> Equipment = new();
    public List<BodyPartModifier> Modifiers = new();
    public int TicksSinceLastHit = int.MaxValue;
    public BodyPartDef BodyPartDef => (BodyPartDef)Def;

    public override string Label => _adaptedLabel ?? "failed to adapt label";
    public Texture2D WhiteIcon => BodyPartDef.WhiteIcon;
    public BodyPartType Type => BodyPartDef.BodyPartType;
    public float Size => BodyPartDef.Size;
    public float HitWeight => BodyPartDef.HitWeight;
    public float HealthPercent => HitPoints / MaxHitPoints;
    public bool IsExternal => Socket?.IsExternal ?? true;
    public bool IsBone => BodyPartDef.IsBone;
    public bool IsOrgan => BodyPartDef.IsOrgan;
    public bool IsVital => BodyPartDef.IsVital;
    public new bool IsDestroyed => HitPoints <= .1f;
    public bool IsBleeding => HealthPercent < .99; //todo coagulation 

    public List<EquipmentSlotType>? EquipmentSlots => BodyPartDef.EquipmentSlots;

    public bool HasEquipmentSlots => BodyPartDef.EquipmentSlots?.Count > 0;

    public Texture2D? Image
    {
        get
        {
            if (_image == null)
            {
                string name = Label.Replace(" ", "");
                foreach (var texturePath in BodyPartDef.BodyTexturePaths)
                {
                    string pathName = texturePath.Split("/").Last();
                    if (name == pathName)
                    {
                        _image = TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(texturePath));
                        break;
                    }
                }
            }

            return _image;
        }
    }

    public float HitPoints
    {
        get => _hitPoints;
        set
        {
            _hitPoints = Mathf.Clamp(value, 0f, MaxHitPoints);
            if (Body != null)
            {
                Body.BodyPartsDirty = true;
            }
        }
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
            if (Type == BodyPartType.Artery && HitPoints <= 0)
            {
                return false;
            }

            if (IsExternal && InternalParts.Any(part => part.Type == BodyPartType.Artery && part.HitPoints <= 0))
            {
                return false;
            }

            return Socket?.ParentPart?.IsArteryFunctional ?? true;
        }
    }

    public bool HasBones
    {
        get { return AllInternalParts.Any(part => part.IsBone); }
    }

    public bool HasBrokenBones
    {
        get { return AllInternalParts.Any(part => part.IsBone && part.HitPoints <= 0); }
    }

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
            if (Modifiers.Any(m => m.Def == Defs.BodyPartModifiers.PumpinEnhancement))
            {
                points *= 2;
            }

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
            List<BodyPart> parts = new();
            foreach (BodyPartSocket socket in Sockets)
            {
                if (socket.AttachedPart?.IsExternal == true)
                {
                    parts.Add(socket.AttachedPart);
                }
            }

            return parts;
        }
    }

    public List<BodyPart> InternalParts
    {
        get
        {
            List<BodyPart> parts = new();
            foreach (BodyPartSocket socket in Sockets)
            {
                if (socket.AttachedPart?.IsExternal == false)
                {
                    parts.Add(socket.AttachedPart);
                }
            }

            return parts;
        }
    }

    public List<BodyPart> AllInternalParts
    {
        get
        {
            List<BodyPart> parts = new();
            GetParts(this, parts, false);
            return parts;
        }
    }

    public Item? Armor
    {
        get
        {
            foreach (Item? item in Equipment.Values)
            {
                if (item?.ItemDef.EquipmentProperties.EquipmentType == EquipmentType.Armor)
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

    public override void Tick(int ticks)
    {
        for (int index = Modifiers.Count - 1; index >= 0; index--)
        {
            BodyPartModifier modifier = Modifiers[index];
            modifier.Tick();
            TicksSinceLastHit++;
            if (modifier.IsExpired)
            {
                modifier.Expired();
                Modifiers.Remove(modifier);
            }
        }

        base.Tick(ticks);
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

    public float ApplyDamage(float damage, DamageType damageType, List<BodyPartModifierRecord> bodyPartModifiers,
        List<DamagedBodyPartRecord> damagedParts, bool cascade = true)
    {
        TicksSinceLastHit = 0;
        var wasDestroyedBeforeDamage = IsDestroyed;
        var wasFunctional = IsFunctional;

        // Do damage scale here
        var scaledDamage = damage;
        if (damageType == DamageType.Blunt && IsBone)
        {
            scaledDamage *= 1.5f;
        }

        var damageApplied = HitPoints;
        HitPoints -= scaledDamage;
        damageApplied -= HitPoints;
        var remainingDamage = damage - damageApplied;
        remainingDamage = damage * 0.7f;

        var wasDestroyed = wasDestroyedBeforeDamage == false && IsDestroyed;
        var stoppedFunctioning = wasFunctional && IsFunctional == false;

        var record = new DamagedBodyPartRecord(this)
        {
            DamageApplied = damageApplied,
            WasDestroyed = wasDestroyed,
            StoppedFunctioning = stoppedFunctioning
        };
        this.ApplyBodyPartModifiers(bodyPartModifiers, record);
        damagedParts.Add(record);
        if (remainingDamage > 0 && cascade)
        {
            remainingDamage = this.CascadeDamageToInternalParts(remainingDamage, damageType, bodyPartModifiers, damagedParts);
        }

        return remainingDamage;
    }

    public List<DamagedBodyPartRecord> ApplyDamageToExternalPart(Damage damage, List<DamagedBodyPartRecord>? damagedParts = null)
    {
        damagedParts ??= [];
        var remainingDamage = ApplyDamage(damage.TotalUnblockedDamage, damage.Type, damage.BodyPartModifiers, damagedParts, false);
        var skin = InternalParts.Where(p => p.Type == BodyPartType.Skin).FirstOrNull();
        skin?.ApplyDamage(damage.TotalUnblockedDamage * SKIN_DAMAGE_SCALER, damage.Type, damage.BodyPartModifiers, damagedParts, false);

        // Cascade damage to internal parts
        if (remainingDamage > 0)
        {
            this.CascadeDamageToInternalParts(remainingDamage, damage.Type, damage.BodyPartModifiers, damagedParts);
        }

        this.PotentiallySevereLimb();
        damagedParts[0].WasSevered = IsSevered;
        PartDamaged?.Invoke(this, damagedParts);
        return damagedParts;
    }

    public bool DidPawnDieFromPartFailure()
    {
        if (this is { IsVital: true, IsFunctional: false })
        {
            if (Body == null)
            {
                return true;
            }

            if (Body!.AllParts.Any(p => p.Type == Type && p.IsFunctional) == false)
            {
                return true;
            }
        }

        if (IsFunctional) return false;

        foreach (var internalPart in InternalParts)
        {
            if (internalPart.DidPawnDieFromPartFailure())
            {
                return true;
            }
        }

        return false;
    }

    public void Severe()
    {
        if (Socket != null)
        {
            Socket.Body = null; //todo not that it matters, but this should probably also set Pawn.Body.RootSocket = null as well
            Socket.AttachedPart = null;
            Socket.IsSealed = false;
            Socket = null;
        }

        IsSevered = true;
    }

    public void TryAddModifier(BodyPartModifier modifer)
    {
        //Log.Debug($"Attempting to apply BodyPartModifier: {modifer.Label} to {this}");
        BodyPartModifier? existingModifier = Modifiers.FirstOrNull(m => m?.Def == modifer.Def);
        if (existingModifier != null)
        {
            existingModifier.MergeWith(modifer);
            return;
        }

        modifer.BodyPart = this;
        Modifiers.Add(modifer);
    }

    public bool HasModifer(BodyPartModifierDef def)
    {
        return Modifiers.Any(m => m.Def == def);
    }

    #region Equipment

    public EquipmentSlotType? SlotFor(Item item)
    {
        var slot = item.ItemDef.EquipmentProperties.SlotUsedToEquip;
        if (slot == null || EquipmentSlots == null) return null;
        return EquipmentSlots.Contains(slot.Value) ? slot.Value : null;
    }

    public EquipmentSlotType? EmptySlotFor(Item item)
    {
        if (!HasEquipmentSlots) return null;

        if (item.ItemDef.ItemType == ItemType.Potion)
        {
            foreach (EquipmentSlotType potionSlot in EquipmentSlots!.Where(s => s is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2))
            {
                if (Equipment[potionSlot] == null)
                {
                    return potionSlot;
                }
            }

            return null;
        }

        var slot = item.ItemDef.EquipmentProperties.SlotUsedToEquip;
        if (slot == null) return null;
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
            MaxHitPoints = Mathf.FloorToInt(MaxHitPoints * Body.BodySizeFactor);
            HitPoints = MaxHitPoints;
        }

        if (BodyPartDef.AdaptiveProperties == null)
        {
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

    private string GenerateLabel()
    {
        string label = "";
        if (Socket?.Def.Position != null)
        {
            label += Socket?.ParentPart?.Position == null ? "" : string.Join(" ", Regex.Split(Socket?.ParentPart?.Position.ToString()!, @"(?<!^)(?=[A-Z])")) + " ";
        }

        label += Position == null ? "" : string.Join(" ", Regex.Split(Position.ToString()!, @"(?<!^)(?=[A-Z])")) + " ";
        label += BodyPartDef.BodyPartType;
        return label;
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hitPoints, "HitPoints");
        ScribeValues.Look(ref _adaptedLabel!, "AdaptedLabel");
        ScribeValues.Look(ref _isSevered, "IsSevered");
        ScribeValues.Look(ref MaxHitPoints, "MaxHitPoints");
        ScribeValues.Look(ref TicksSinceLastHit, "TicksSinceLastHit");
        ScribeReferences.Look(ref Socket!, "Socket");
        ScribeCollections.Look(ref Sockets!, "Sockets", LookMode.Deep);
        ScribeCollections.Look(ref Modifiers!, "Modifiers", LookMode.Deep);
        ScribeCollections.Look(ref Equipment!, "Equipment", LookMode.Value, LookMode.Deep);
        base.ExposeData();
    }
}