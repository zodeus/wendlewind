using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PawnEquipment : IEnumerable<Item>, IExposable
{
    private readonly Pawn _pawn;

    //public Dictionary<BodyPart, List<Item>> Items;
    public IEnumerable<Item> Armor
    {
        get
        {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts)
            {
                foreach (Item? item in externalPart.Equipment.Values)
                {
                    if (item != null && item.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Armor)
                    {
                        yield return item;
                    }
                }
            }
        }
    }

    public IEnumerable<(Item, BodyPart)> Weapons
    {
        get
        {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts)
            {
                foreach (Item? item in externalPart.Equipment.Values)
                {
                    if (item != null && item.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Weapon)
                    {
                        yield return (item, externalPart);
                    }
                }
            }
        }
    }

    public IEnumerable<Item> Potions
    {
        get
        {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts)
            {
                foreach (Item? item in externalPart.Equipment.Values)
                {
                    if (item?.ItemDef.ItemType == ItemType.Potion)
                    {
                        yield return item;
                    }
                }
            }
        }
    }

    public IEnumerable<Item> UsableItems
    {
        get
        {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts)
            {
                if (externalPart.HasMobility == false)
                {
                    continue;
                }

                foreach (Item? item in externalPart.Equipment.Values)
                {
                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }
        }
    }

    public IEnumerable<Item> UsableWeapons => UsableItems.Where(i => i.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Weapon);


    public IEnumerable<KeyValuePair<BodyPart, List<EquipmentSlotType>>> Slots
    {
        get
        {
            foreach (BodyPart externalPart in _pawn.Body.AllExternalParts)
            {
                yield return new KeyValuePair<BodyPart, List<EquipmentSlotType>>(externalPart, externalPart.Equipment.Keys.ToList());
            }
        }
    }

    public PawnEquipment(Pawn pawn)
    {
        _pawn = pawn;
    }

    public Item? TryEquip(BodyPart bodyPart, Item item)
    {
        return TryEquip(bodyPart, bodyPart.SlotFor(item)!.Value, item);
    }

    public Item? TryEquip(BodyPart bodyPart, EquipmentSlotType slot, Item item)
    {
        if (item.ItemDef.EquipmentProperties?.SlotUsedToEquip == null)
        {
            Log.Error($"Tried to equip '{item}' but SlotUsedToEquip is null");
            return null;
        }

        Item? unequippedItem = UnEquip(bodyPart, slot);
        item.EjectFromContainer();
        bodyPart.Equipment[slot] = item;

        //OnEquipmentChanged(new OnChangeArgs(OnChangeArgs.ChangeType.ItemEquipped, item));
        return unequippedItem;
    }

    public Item? UnEquip(Item item)
    {
        foreach ((BodyPart? bodyPart, var slots) in Slots)
        {
            foreach (EquipmentSlotType slot in slots)
            {
                if (item == bodyPart.Equipment[slot])
                {
                    return UnEquip(bodyPart, slot);
                }
            }
        }

        return null;
    }

    public Item? UnEquip(BodyPart bodyPart, EquipmentSlotType slot)
    {
        Item? item = GetBySlot(bodyPart, slot);
        if (item == null) return null;
        UnEquipInternal(bodyPart, slot, item);

        return item;
    }

    private void UnEquipInternal(BodyPart bodyPart, EquipmentSlotType slot, Item item)
    {
        bodyPart.Equipment[slot] = null;
        //OnEquipmentChanged(new OnChangeArgs(OnChangeArgs.ChangeType.ItemUnequipped, item));
    }

    public Item? GetBySlot(BodyPart bodyPart, EquipmentSlotType slot)
    {
        if (bodyPart.Equipment.ContainsKey(slot) == false)
        {
            return null;
        }

        return bodyPart.Equipment[slot];
    }

    public void ExposeData()
    {
    }

    public IEnumerator<Item> GetEnumerator()
    {
        foreach (BodyPart externalPart in _pawn.Body.AllExternalParts)
        {
            foreach (Item? item in externalPart.Equipment.Values)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Item? PotionByDef(ItemDef potionDef)
    {
        foreach (Item potion in Potions)
        {
            if (potion.Def == potionDef)
            {
                return potion;
            }
        }

        return null;
    }

    public int SlotCountFor(ItemDef itemDef)
    {
        return Slots.Sum(slot => slot.Value.Count(slotType => itemDef.EquipmentProperties?.SlotUsedToEquip == slotType));
    }

    public void Tick()
    {
        foreach (Item item in this)
        {
            var bodyPart = _pawn.Body.AllExternalParts.FirstOrDefault(p => p.Equipment.Values.Contains(item));
            if (bodyPart == null)
            {
                continue;
            }

            item.EquipmentHandler?.Tick(_pawn, bodyPart);
            
            if (item.Enchantments != null)
            {
                foreach (var enchantment in item.Enchantments)
                {
                    enchantment.EnchantmentHandler?.TickForPawn(_pawn, bodyPart);
                }
            }
        }
    }
}