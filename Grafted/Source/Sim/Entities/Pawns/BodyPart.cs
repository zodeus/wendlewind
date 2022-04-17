using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPart : Entity {
    public List<BodyPartSocket> Sockets = new();

    private int _hitPoints;

    private bool _isSevered; // todo, this should be set by an applied health condition

    public int MaxHitPoints;

    //public List<HealthCondition> HealthConditions = new();

    public BodyPartSocket? Socket = null;

    public Dictionary<EquipmentSlotType, Item?> Equipment = new();

    public BodyPartDef BodyPartDef => (BodyPartDef) Def;
    public BodyPartType Type => BodyPartDef.BodyPartType;
    public float Size => BodyPartDef.Size;
    public float HealthPercent => (float) HitPoints / MaxHitPoints;
    public bool IsExternal => Socket?.IsExternal ?? true;
    public bool IsBone => BodyPartDef.IsBone;
    public bool IsOrgan => BodyPartDef.IsOrgan;
    public bool IsVital => BodyPartDef.IsVital;

    public bool IsDestroyed => HitPoints <= 0;

    // IsCoagulated
    public bool IsBleeding => TicksSinceLastHit < 2;

    public int TicksSinceLastHit = int.MaxValue;

    public List<EquipmentSlotType>? EquipmentSlots => BodyPartDef.EquipmentSlots;

    public bool HasEquipmentSlots => BodyPartDef.EquipmentSlots?.Count > 0;

    public int HitPoints {
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

    public Item? Armor {
        get {
            foreach (Item? item in Equipment.Values) {
                if (item?.ItemDef.EquipmentProperties.EquipmentType == EquipmentType.Armor) {
                    return item;
                }
            }

            return null;
        }
    }

    #endregion

    public override void Initialize() {
        base.Initialize();
        MaxHitPoints = (int) this.GetStatValue(Defs.Stats.MaxHitPoints);
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
            float chanceToMiss = Socket?.ParentPart?.HealthPercent switch {
                < .10f => 0.00f,
                < .20f => 0.50f,
                < .40f => 0.95f,
                < .80f => 0.99f,
                _ => 1
            };

            if (Core.Random.Chance(chanceToMiss)) {
                return damagedParts;
            }
        }

        if (Type is BodyPartType.Stomach && Socket?.ParentPart?.HealthPercent > 0.5) {
            return damagedParts;
        }

        if (Type == BodyPartType.Artery) {
            float chanceToMiss = Socket?.ParentPart?.HealthPercent switch {
                < .02f => 0.00f,
                < .05f => 0.85f,
                < .10f => 0.90f,
                < .50f => 0.95f,
                < .90f => 0.99f,
                _ => 1
            };

            if (Core.Random.Chance(chanceToMiss)) {
                return damagedParts;
            }
        }

        int partDamage = damage.UnblockedAmount;
        if (damage.Type == DamageType.Blunt && IsBone) {
            partDamage = Mathf.RoundToInt(partDamage * 1.5f);
        }

        HitPoints -= partDamage;
        TicksSinceLastHit = 0;
        damagedParts.Add(new DamagedPartRecord(this, partDamage));

        int organsHit = 0;
        int maxNumberOfOrgansToHit = Core.Random.Next(1, 2);
        foreach (BodyPart internalPart in InternalParts.InRandomOrder()) {
            if (damage.Type == DamageType.Flesh && internalPart is not { Type: BodyPartType.Bone or BodyPartType.Skin }) {
                continue;
            }

            if (internalPart.IsOrgan && organsHit > maxNumberOfOrgansToHit) {
                continue;
            }

            if (internalPart.IsOrgan) {
                organsHit++;
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
                //damagedPartRecord.WasSevered = true;
            }
        }

        return damagedParts;
    }

    public void Severe() {
        if (Socket != null) {
            Socket.AttachedPart = null;
            Socket.IsSealed = false;
            Socket = null;
        }

        IsSevered = true;
    }

    #region Equipment

    public EquipmentSlotType? SlotFor(Item item) {
        var slot = item.ItemDef.EquipmentProperties.SlotUsedToEquip;
        if (slot == null || EquipmentSlots == null) return null;
        return EquipmentSlots.Contains(slot.Value) ? slot.Value : null;
    }

    public EquipmentSlotType? EmptySlotFor(Item item) {
        if (item.ItemDef.ItemType == ItemType.Potion) {
            if (!HasEquipmentSlots) return null;
            foreach (EquipmentSlotType potionSlot in EquipmentSlots!.Where(s => s is EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2)) {
                if (Equipment[potionSlot] == null) {
                    return potionSlot;
                }
            }

            return null;
        }

        var slot = item.ItemDef.EquipmentProperties.SlotUsedToEquip;
        if (slot == null || EquipmentSlots == null) return null;
        return EquipmentSlots.Contains(slot.Value) ? slot.Value : null;
    }

    #endregion

    public Item? UnEquip(Item itemToUnEquip) {
        foreach ((EquipmentSlotType slot, Item? item) in Equipment) {
            if (item == itemToUnEquip) {
                Equipment[slot] = null;
                return item;
            }
        }

        return null;
    }
}