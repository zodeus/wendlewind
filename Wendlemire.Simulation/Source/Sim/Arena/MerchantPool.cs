namespace Wendlemire.Sim.Arena;

public static class MerchantPool
{
    public static IReadOnlyList<MerchantDef> Available(int fightsPlayed)
    {
        var exclusive = ExclusiveMerchant(fightsPlayed);
        if (exclusive != null)
        {
            return [exclusive];
        }

        return DefRepository<MerchantDef>.Defs.Where(merchant => !merchant.IsGeneralStore).ToList();
    }

    public static MerchantDef Select(int runSeed, int fightsPlayed)
    {
        var pool = Available(fightsPlayed);
        if (pool.Count == 0)
        {
            return Defs.Merchants.Blacksmith;
        }

        if (pool.Count == 1)
        {
            return pool[0];
        }

        var rng = new Random(ArenaSeeds.Merchant(runSeed, fightsPlayed));
        return pool[rng.Next(pool.Count)];
    }

    private static MerchantDef? ExclusiveMerchant(int fightsPlayed) => fightsPlayed switch
    {
        1 => Defs.Merchants.Blacksmith,
        2 => Defs.Merchants.Ranger,
        3 => Defs.Merchants.Alchemist,
        4 => Defs.Merchants.Magician,
        8 => Defs.Merchants.Blacksmith,
        10 => Defs.Merchants.Magician,
        _ => null
    };
}
