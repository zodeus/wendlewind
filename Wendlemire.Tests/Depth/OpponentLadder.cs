using System.Collections.Concurrent;
using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Arena;

namespace Wendlemire.Tests.Depth;

internal static class OpponentLadder
{
    private static readonly ConcurrentDictionary<(int Seed, int Round), BuildSnapshot> Cache = new();

    public static BuildStage StageFor(int round) => round switch
    {
        <= 3 => BuildStage.Early,
        <= 6 => BuildStage.Mid,
        <= 9 => BuildStage.Late,
        _ => BuildStage.End
    };

    public static BuildSnapshot For(int runSeed, int round)
    {
        return Cache.GetOrAdd((runSeed, round), key =>
        {
            var rng = new Random(unchecked(key.Seed * 397 ^ key.Round * 7919 ^ 0x5F3759DF));
            var archetypes = (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype));
            var archetype = archetypes[rng.Next(archetypes.Length)];
            return ShopWalk(key.Seed, key.Round, archetype);
        });
    }

    public static BuildSnapshot Generate(
        BuildStage stage,
        BuildGenerator.Archetype archetype,
        int index,
        int seed)
    {
        var rng = new Random(unchecked(seed * 397 ^ (int)stage * 7919 ^ (int)archetype * 104729 ^ index));
        return Tag(BuildGenerator.Generate(stage, archetype, index, rng), $"{archetype}-{stage}-{index}");
    }

    public static BuildSnapshot Tag(BuildSnapshot snapshot, string playerId) =>
        snapshot with { PlayerId = playerId };

    private static BuildSnapshot ShopWalk(int runSeed, int round, BuildGenerator.Archetype archetype)
    {
        using var scope = new ArenaContextScope("ladder", $"Opp {archetype}", runSeed);
        var ctx = scope.Context;
        var run = ctx.ArenaRun ?? throw new InvalidOperationException("Arena run was not initialized.");
        var policy = ShopPolicies.Planned(archetype);
        var rng = new Random(unchecked(runSeed * 1103515245 + (int)archetype * 7919 + round));

        for (var visit = 1; visit <= round; visit++)
        {
            ctx.RefreshPlayerConsumableSlots();
            var merchant = run.CurrentMerchant
                           ?? DefRepository<MerchantDef>.GetByMoniker("GeneralStore")!;
            run.CurrentMerchant = merchant;
            var rolled = ShopStock.Roll(
                merchant,
                run.RunSeed,
                run.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(ctx.Player));
            var shelves = run.OpenShopVisit(merchant, rolled);
            policy.Shop(run, ctx, shelves, rng);

            if (visit < round)
            {
                run.ApplyMatchResult(true, $"ladder-bye-{visit}");
            }
        }

        var snapshot = BuildSnapshotFactory.ToSnapshot(
            ctx.PlayerPawn,
            $"opp-r{round}",
            $"ladder-{archetype}-r{round}-{runSeed}",
            runSeed,
            round);
        return snapshot with
        {
            PlayerId = $"opp-r{round}",
            GoldSpent = BuildGenerator.ComputeGoldSpent(snapshot),
            Round = round
        };
    }
}
