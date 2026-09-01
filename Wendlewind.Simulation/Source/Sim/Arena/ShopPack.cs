namespace Wendlewind.Sim.Arena;

public static class ShopPack
{
    public static IEnumerable<(Item Item, bool Equipped)> SellableItems(Pawn pawn)
    {
        var seen = new HashSet<Item>();
        foreach (var item in pawn.Equipment)
        {
            if (!IsSellable(item) || IsBuiltin(item) || !seen.Add(item))
            {
                continue;
            }

            yield return (item, true);
        }

        foreach (var item in pawn.Inventory)
        {
            if (!IsSellable(item) || !seen.Add(item))
            {
                continue;
            }

            yield return (item, false);
        }
    }

    public static bool IsSellable(Item item) =>
        item is { IsDestroyed: false } && ShopCatalog.GetBuyPrice(item.ItemDef) > 0;

    public static bool IsBuiltin(Item item) =>
        item.ItemDef.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.BuiltIn;
}
