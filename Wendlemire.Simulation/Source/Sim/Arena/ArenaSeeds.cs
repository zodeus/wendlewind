namespace Wendlemire.Sim.Arena;

public static class ArenaSeeds
{
    public static int Shop(int runSeed, string merchantMoniker, int visit) =>
        SeedUtility.Mix(runSeed, SeedUtility.StableHash($"shop:{merchantMoniker}"), visit);

    public static int ShopRefresh(
        int runSeed,
        string merchantMoniker,
        int visit,
        ShopCategory category,
        int refreshIndex) =>
        SeedUtility.Mix(
            SeedUtility.Mix(runSeed, SeedUtility.StableHash($"shop-refresh:{merchantMoniker}:{category}"), visit),
            refreshIndex);

    public static int Encounter(int runSeed, int fightNumber) =>
        SeedUtility.Mix(runSeed, SeedUtility.StableHash("arena-fight"), fightNumber);

    public static int Event(int runSeed, string eventKey) =>
        SeedUtility.Mix(runSeed, SeedUtility.StableHash($"event:{eventKey}"));
}
