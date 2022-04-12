using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Persistence;
using Grafted.Utils;

namespace Grafted.Sim.Entities.Pawns;

public class PawnEquipment : IEnumerable<Item>, IExposable {
    private readonly Pawn _pawn;

    //public Dictionary<BodyPart, List<Item>> Items;
    public IEnumerable<Item> Armor {
        get {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts) {
                foreach (Item? item in externalPart.Equipment.Values) {
                    if (item != null && item.ItemDef.EquipmentProperties.EquipmentType == EquipmentType.Armor) {
                        yield return item;
                    }
                }
            }
        }
    }

    public IEnumerable<Item> Potions => new List<Item>();

    public IEnumerable<Item> UsableItems {
        get {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts) {
                if (externalPart.HasMobility == false) {
                    continue;
                }

                foreach (Item? item in externalPart.Equipment.Values) {
                    if (item != null) {
                        yield return item;
                    }
                }
            }
        }
    }

    public IEnumerable<KeyValuePair<BodyPart, List<EquipmentSlotType>>> Slots {
        get {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts) {
                yield return new KeyValuePair<BodyPart, List<EquipmentSlotType>>(externalPart, externalPart.Equipment.Keys.ToList());
            }
        }
    }

    public PawnEquipment(Pawn pawn) {
        _pawn = pawn;
    }

    public void Tick() { }

    public Item? TryEquip(BodyPart bodyPart, Item item) {
        /*if (item.ItemDef.ItemType == ItemType.Potion) {
            Item? potion = EquipPotion(item);
            if (potion != null) {
                //return potion, failed to equip
                return new[] { potion };
            }
    
            return Array.Empty<Item>();
        }*/

        if (item.ItemDef.EquipmentProperties.SlotUsedToEquip == null) {
            Log.Error($"Tried to equip '{item}' but SlotUsedToEquip is null");
            return null;
        }

        var slot = bodyPart.SlotFor(item);
        if (slot == null) {
            Log.Error($"Tried to equip '{item}' we don't have the slots");
            return null;
        }

        Item? unequippedItem = UnEquip(bodyPart, slot.Value);
        bodyPart.Equipment[slot.Value] = item;
        //OnEquipmentChanged(new OnChangeArgs(OnChangeArgs.ChangeType.ItemEquipped, item));
        return unequippedItem;
    }


    public Item? UnEquip(BodyPart bodyPart, EquipmentSlotType slot) {
        Item? item = GetBySlot(bodyPart, slot);
        if (item == null) return null;
        UnEquipInternal(bodyPart, item);

        return item;
    }

    private void UnEquipInternal(BodyPart bodyPart, Item? item) {
        if (item?.ItemDef.EquipmentProperties.SlotUsedToEquip is not { } slot) {
            return;
        }

        if (item.ItemDef.ItemType == ItemType.Potion) {
            //_potions.Remove(item);
        }
        else {
            bodyPart.Equipment[slot] = null;
        }

        //OnEquipmentChanged(new OnChangeArgs(OnChangeArgs.ChangeType.ItemUnequipped, item));
    }

    public Item? GetBySlot(BodyPart bodyPart, EquipmentSlotType slot) {
        if (bodyPart.Equipment.ContainsKey(slot) == false) {
            return null;
        }

        return bodyPart.Equipment[slot];
    }

    public void ExposeData() { }

    public IEnumerator<Item> GetEnumerator() {
        foreach (BodyPart externalPart in _pawn.Body.AllExternalParts) {
            foreach (Item? item in externalPart.Equipment.Values) {
                if (item != null) {
                    yield return item;
                }

            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}