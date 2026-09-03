namespace Wendlemire.Sim.Entities.Items.Enchantments;

public static class EnchantmentSocketing
{
    public static IEnumerable<Item> UnequippedEnchantments(Pawn pawn)
    {
        foreach (var item in pawn.Inventory)
        {
            if (!item.IsDestroyed && item.StackSize > 0 && item.ItemDef.ItemType == ItemType.Enchantment)
            {
                yield return item;
            }
        }
    }

    public static bool CanSocket(Item host, Item enchantment)
    {
        if (enchantment.IsDestroyed || enchantment.ItemDef.ItemType != ItemType.Enchantment)
        {
            return false;
        }

        return HostAccepts(host, enchantment.ItemDef);
    }

    private static bool HostAccepts(Item host, ItemDef enchantment)
    {
        if (host.IsDestroyed || enchantment.ItemType != ItemType.Enchantment)
        {
            return false;
        }

        var type = host.ItemDef.EquipmentProperties?.EquipmentType;
        if (type is not (EquipmentType.Armor or EquipmentType.Weapon))
        {
            return false;
        }

        if (enchantment.EnchantmentProperties?.ValidEquipmentTypes.Contains(type.Value) != true)
        {
            return false;
        }

        return host.Enchantments?.HasEmptySocket() == true;
    }

    public static bool HostAcceptsUnequipped(Pawn pawn, Item host)
    {
        foreach (var enchantment in UnequippedEnchantments(pawn))
        {
            if (CanSocket(host, enchantment))
            {
                return true;
            }
        }

        return false;
    }

    public static bool EnchantmentHasCompatibleHost(Pawn pawn, Item enchantment) =>
        EnchantmentHasCompatibleHost(pawn, enchantment.ItemDef);

    public static bool EnchantmentHasCompatibleHost(Pawn pawn, ItemDef enchantment)
    {
        foreach (var item in pawn.Equipment)
        {
            if (HostAccepts(item, enchantment))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TrySocket(Item host, Item enchantment)
    {
        if (!CanSocket(host, enchantment) || host.Enchantments == null)
        {
            return false;
        }

        if (!host.Enchantments.TryGetEmptySocket(out var index))
        {
            return false;
        }

        enchantment.EjectFromContainer();
        host.Enchantments.TryAdd(enchantment, index);
        return true;
    }
}
