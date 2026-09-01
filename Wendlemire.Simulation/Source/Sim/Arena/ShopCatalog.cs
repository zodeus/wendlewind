namespace Wendlemire.Sim.Arena;

public static class ShopCatalog
{
    public const float SetDiscount = 0.8f;
    public const int SellDivisor = 3;
    public const int ShelfRefreshBaseCost = 10;

    public static int GetBuyPrice(ItemDef def) => def.GoldCost;

    public static int ShelfRefreshCost(ShopCategory category, int refreshCount)
    {
        var paidCount = refreshCount;
        if (category == ShopCategory.Armor)
        {
            if (refreshCount <= 0)
            {
                return 0;
            }

            paidCount = refreshCount - 1;
        }

        var count = Math.Clamp(paidCount, 0, 20);
        return ShelfRefreshBaseCost * (1 << count);
    }

    public static int GetSellPrice(ItemDef def) => def.GoldCost / SellDivisor;

    public static int ComputeSetCost(IReadOnlyList<ItemDef> pieces)
    {
        var sum = 0;
        foreach (var piece in pieces)
        {
            sum += piece.GoldCost;
        }

        return (int)Math.Floor(sum * SetDiscount);
    }
}
