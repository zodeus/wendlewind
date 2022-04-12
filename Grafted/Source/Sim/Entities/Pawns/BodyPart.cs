using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPart : Entity {
    public List<BodyPartSocket> Sockets = new();

    private float _hitPoints;

    private bool _isSevered; // todo, this should be set by an applied health condition

    public float MaxHitPoints;

    //public List<HealthCondition> HealthConditions = new();

    public BodyPartSocket Socket = null!;

    public Dictionary<EquipmentSlotType, Item?> Equipment = new();

    public BodyPartDef BodyPartDef => (BodyPartDef) Def;
    public BodyPartType Type => BodyPartDef.BodyPartType;
    public float Size => BodyPartDef.Size;
    public float HealthPercent => HitPoints / MaxHitPoints;
    public bool IsExternal => Socket?.IsExternal ?? true;
    public bool IsBone => BodyPartDef.IsBone;
    public bool IsVital => BodyPartDef.IsVital;

    public bool IsDestroyed => HitPoints <= 0;

    // IsCoagulated
    public bool IsBleeding => TicksSinceLastHit < 2;

    public int TicksSinceLastHit = int.MaxValue;

    public List<EquipmentSlotType>? EquipmentSlots => BodyPartDef.EquipmentSlots;

    public bool HasEquipmentSlots => BodyPartDef.EquipmentSlots?.Count > 0;

    public float HitPoints {
        get => _hitPoints;
        set => _hitPoints = Mathf.Clamp(value, 0, MaxHitPoints);
    }

    #region Dynamic Getters

    public bool IsSevered {
        get {
            if (_isSevered) {
                return true;
            }

            return Socket?.ParentPart?.IsSevered ?? false;
        }
        private set => _isSevered = value;
    }

    public bool IsArteryFunctional {
        get {
            if (Type == BodyPartType.Artery && HitPoints <= 0) {
                return false;
            }

            if (IsExternal && InternalParts.Any(part => part.Type == BodyPartType.Artery && part.HitPoints <= 0)) {
                return false;
            }

            return Socket?.ParentPart?.IsArteryFunctional ?? true;
        }
    }

    public bool HasBones {
        get { return AllInternalParts.Any(part => part.IsBone); }
    }

    public bool HasBrokenBones {
        get { return AllInternalParts.Any(part => part.IsBone && part.HitPoints <= 0); }
    }

    public bool HasMobility {
        get {
            if (IsDestroyed) {
                return false;
            }

            if (HasBrokenBones) {
                return false;
            }

            if (IsArteryFunctional == false) {
                return false;
            }

            if (Socket?.ParentPart is { HasMobility: false }) {
                return false;
            }

            return true;
        }
    }

    public bool IsFunctional {
        get {
            if (IsDestroyed) {
                return false;
            }

            if (IsArteryFunctional == false) {
                return false;
            }

            if (Socket?.ParentPart is { IsFunctional: false }) {
                return false;
            }

            return true;
        }
    }


    public List<BodyPart> ExternalParts {
        get {
            List<BodyPart> parts = new();
            foreach (BodyPartSocket socket in Sockets) {
                if (socket.AttachedPart?.IsExternal == true) {
                    parts.Add(socket.AttachedPart);
                }
            }

            return parts;
        }
    }

    public List<BodyPart> InternalParts {
        get {
            List<BodyPart> parts = new();
            foreach (BodyPartSocket socket in Sockets) {
                if (socket.AttachedPart?.IsExternal == false) {
                    parts.Add(socket.AttachedPart);
                }
            }

            return parts;
        }
    }

    public List<BodyPart> AllInternalParts {
        get {
            List<BodyPart> parts = new();
            GetParts(this, parts, false);
            return parts;
        }
    }

    public IEnumerable<Item> Armor {
        get {
            foreach (Item? item in Equipment.Values) {
                if (item?.ItemDef.EquipmentProperties.EquipmentType == EquipmentType.Armor) {
                    yield return item;
                }
            }
        }
    }

    #endregion

    public override void Initialize() {
        base.Initialize();
        MaxHitPoints = this.GetStatValue(Defs.Stats.MaxHitPoints);
        HitPoints = MaxHitPoints;

        //Register Sockets
        foreach (BodyPartSocketDef bodyPartSocketDef in BodyPartDef.Sockets) {
            Sockets.Add(new BodyPartSocket(bodyPartSocketDef, this));
        }

        if (BodyPartDef.EquipmentSlots != null) {
            foreach (EquipmentSlotType slot in BodyPartDef.EquipmentSlots) {
                Equipment.Add(slot, null);
            }
        }
    }

    private void GetParts(BodyPart part, List<BodyPart> parts, bool? partIsExternal = null) {
        foreach (BodyPartSocket socket in part.Sockets) {
            if (partIsExternal != null && socket.AttachedPart?.IsExternal == partIsExternal) {
                parts.Add(socket.AttachedPart);
                GetParts(socket.AttachedPart, parts, partIsExternal);
            }
        }
    }

    public List<BodyPartSocket> GetSocketsFor(BodyPartType bodyPartType) {
        List<BodyPartSocket> sockets = new();
        foreach (BodyPartSocket bodyPartSocket in Sockets) {
            if (bodyPartSocket.CanSocket(bodyPartType)) {
                sockets.Add(bodyPartSocket);
            }
        }

        return sockets;
    }

    public override string ToString() {
        return $"{Label} ({HitPoints:0.000})";
    }

    public List<DamagedPartRecord> ApplyDamage(Damage damage, List<DamagedPartRecord>? damagedParts = null) {
        if (damagedParts == null) {
            damagedParts = new List<DamagedPartRecord>();
        }

        if (Socket?.ParentPart?.HitPoints > 0 && Socket?.ParentPart?.Type is BodyPartType.Skull or BodyPartType.RibCage) {
            return damagedParts;
        }

        if (Type is BodyPartType.Stomach && Socket?.ParentPart?.HealthPercent > 0.5) {
            return damagedParts;
        }

        if (Type == BodyPartType.Artery) {
            float chanceToMiss = Socket?.ParentPart?.HealthPercent switch {
                < .05f => 0.00f,
                < .10f => 0.30f,
                < .25f => 0.60f,
                < .50f => 0.80f,
                < .70f => 0.95f,
                < .90f => 0.99f,
                _ => 1
            };

            if (Core.Random.Chance(chanceToMiss)) {
                return damagedParts;
            }
        }

        float partDamage = damage.UnblockedAmount;
        if (damage.Type == DamageType.Blunt && IsBone) {
            partDamage *= 2;
        }

        bool alreadyDestroyed = IsDestroyed;
        HitPoints -= partDamage;
        TicksSinceLastHit = 0;

        DamagedPartRecord damagedPartRecord = new(this, partDamage);

        if (HitPoints < 0 && alreadyDestroyed == false) {
            damagedPartRecord.WasDestroyed = true;
            HitPoints = 0;
        }

        foreach (BodyPart internalPart in AllInternalParts) {
            if (damage.Type == DamageType.Flesh && internalPart is not { Type: BodyPartType.Bone or BodyPartType.Skin }) {
                continue;
            }

            internalPart.ApplyDamage(damage, damagedParts);
        }

        // Potentially sever limb 
        if (IsExternal && _isSevered == false && AllInternalParts.Count > 0) {
            bool allInternalPartsDestroyed = true;
            foreach (BodyPart internalPart in AllInternalParts) {
                if (internalPart.HitPoints > 0) {
                    allInternalPartsDestroyed = false;
                }
            }

            if (allInternalPartsDestroyed && Socket != null && Core.Random.Chance(.25f)) {
                Severe();
                damagedPartRecord.WasSevered = true;
            }
        }

        damagedParts.Add(damagedPartRecord);

        if (IsVital && HitPoints <= 0) {
            // if part is vital and last one, kill pawn
            Log.Info($"Pawn died because vital body part {this} was destroyed");
        }
        // if part is artery, disable connected external parts, do bleeding

        return damagedParts;
    }

    public void Severe() {
        Socket.AttachedPart = null;
        Socket.IsSealed = false;

        IsSevered = true;
        Log.Info($"Severed limb {this} ({Socket})");
    }

    #region Equipment

    public EquipmentSlotType? SlotFor(Item item) {
        var slot = item.ItemDef.EquipmentProperties.SlotUsedToEquip;
        if (slot == null || EquipmentSlots == null) return null;
        return EquipmentSlots.Contains(slot.Value) ? slot.Value : null;
    }

    #endregion
}