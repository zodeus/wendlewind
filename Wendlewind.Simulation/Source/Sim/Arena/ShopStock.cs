namespace Wendlewind.Sim.Arena;

public static class ShopStock
{
    public static IReadOnlyList<MerchantOffer> Roll(MerchantDef merchant, int runSeed, int wins)
    {
        if (merchant.Offers.Count == 0)
        {
            return [];
        }

        var seed = ArenaSeeds.Shop(runSeed, merchant.Moniker, wins);
        var rng = new Random(seed);
        var pool = merchant.Offers.ToList();
        for (var i = pool.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var take = Math.Min(Math.Max(merchant.StockSize, 1), pool.Count);
        return pool.GetRange(0, take);
    }
}
