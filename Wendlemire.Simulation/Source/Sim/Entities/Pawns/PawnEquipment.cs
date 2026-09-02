using System.Collections;

namespace Wendlemire.Sim.Entities.Pawns;

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

    /// <summary>
    /// Weapons that should strike this attack: every usable held weapon, or builtin
    /// (fist/foot) weapons when nothing else is equipped.
    /// </summary>
    public IEnumerable<Item> CombatWeapons
    {
        get
        {
            var hasHeldWeapon = false;
            foreach (var weapon in UsableWeapons)
            {
                if (IsBuiltinWeapon(weapon))
                {
                    continue;
                }

                hasHeldWeapon = true;
                yield return weapon;
            }

            if (hasHeldWeapon)
            {
                yield break;
            }

            foreach (var weapon in UsableWeapons)
            {
                yield return weapon;
            }
        }
    }


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

        var extras = UnequipConflictingHands(bodyPart, slot, item);
        Item? unequippedItem = UnEquip(bodyPart, slot);
        item.EjectFromContainer();
        bodyPart.Equipment[slot] = item;

        if (item.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Weapon)
        {
            SyncWeaponCombatUse();
        }

        foreach (var extra in extras)
        {
            if (extra != unequippedItem)
            {
                _pawn.Inventory.TryAdd(extra);
            }
        }

        //OnEquipmentChanged(new OnChangeArgs(OnChangeArgs.ChangeType.ItemEquipped, item));
        return unequippedItem;
    }

    public bool HasTwoHandedWeapon()
    {
        foreach (var (weapon, _) in Weapons)
        {
            if (weapon.ItemDef.EquipmentProperties?.OccupiesBothHands == true)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsHandSlotBlockedByTwoHanded(BodyPart bodyPart, EquipmentSlotType slot)
    {
        return TryGetTwoHandedWeaponBlocking(bodyPart, slot, out _);
    }

    public bool TryGetTwoHandedWeapon(out Item weapon, out BodyPart equippedPart)
    {
        foreach (var (item, part) in Weapons)
        {
            if (item.ItemDef.EquipmentProperties?.OccupiesBothHands == true)
            {
                weapon = item;
                equippedPart = part;
                return true;
            }
        }

        weapon = null!;
        equippedPart = null!;
        return false;
    }

    public bool TryGetTwoHandedWeaponBlocking(BodyPart bodyPart, EquipmentSlotType slot, out Item weapon)
    {
        weapon = null!;
        if (slot != EquipmentSlotType.HandWeapon || GetBySlot(bodyPart, slot) != null)
        {
            return false;
        }

        return TryGetTwoHandedWeapon(out weapon, out _);
    }

    private List<Item> UnequipConflictingHands(BodyPart keepPart, EquipmentSlotType keepSlot, Item incoming)
    {
        var extras = new List<Item>();
        var props = incoming.ItemDef.EquipmentProperties;
        if (props?.SlotUsedToEquip != EquipmentSlotType.HandWeapon)
        {
            return extras;
        }

        foreach (var (part, slots) in Slots)
        {
            foreach (var slot in slots)
            {
                if (slot != EquipmentSlotType.HandWeapon)
                {
                    continue;
                }

                if (part == keepPart && slot == keepSlot)
                {
                    continue;
                }

                var equipped = GetBySlot(part, slot);
                if (equipped == null)
                {
                    continue;
                }

                var shouldUnequip = props.OccupiesBothHands
                    || equipped.ItemDef.EquipmentProperties?.OccupiesBothHands == true;
                if (!shouldUnequip)
                {
                    continue;
                }

                var removed = UnEquip(part, slot);
                if (removed != null)
                {
                    extras.Add(removed);
                }
            }
        }

        return extras;
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

        if (item.ItemDef.EquipmentProperties?.EquipmentType == EquipmentType.Weapon)
        {
            SyncWeaponCombatUse();
        }

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

    private static bool IsBuiltinWeapon(Item weapon)
    {
        return weapon.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn;
    }

    public void SyncWeaponCombatUse()
    {
        var hasHeldWeapon = Weapons.Any(w => !IsBuiltinWeapon(w.Item1));
        foreach (var (weapon, _) in Weapons)
        {
            weapon.UseInCombat = IsBuiltinWeapon(weapon) ? !hasHeldWeapon : true;
        }
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