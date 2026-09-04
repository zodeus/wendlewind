using Wendlemire.Definitions;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;

namespace Wendlemire.Tests.Depth;

internal sealed record FightRecord(
    int Round,
    bool Won,
    int GoldAfter,
    int GoldSpent,
    string OpponentId,
    double MatchupWinRate,
    int SnapshotGoldSpent);

internal sealed record RunResult(
    string Policy,
    int RunSeed,
    int Wins,
    int Losses,
    bool Victory,
    bool Terminated,
    int GoldRemaining,
    int LifetimeGold,
    int GoldSpent,
    int Fights,
    bool GoldWentNegative,
    IReadOnlyList<FightRecord> FightLog,
    IReadOnlyDictionary<int, BuildSnapshot> SnapshotsAtRound);

internal static class ArenaRunHarness
{
    public static readonly int[] SnapshotRounds = [2, 5, 8, 12];

    public static RunResult Play(IShopPolicy policy, int runSeed, int fightSeeds = 1)
    {
        using var scope = new ArenaContextScope("depth", policy.Name, runSeed);
        var ctx = scope.Context;
        var run = ctx.ArenaRun ?? throw new InvalidOperationException("Arena run was not initialized.");
        var rng = new Random(unchecked(runSeed * 1103515245 + StableHash(policy.Name)));
        var fights = new List<FightRecord>();
        var snapshots = new Dictionary<int, BuildSnapshot>();
        var goldWentNegative = false;

        while (!run.IsRunOver && run.FightsPlayed < 16)
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
            if (run.Gold < 0)
            {
                goldWentNegative = true;
            }

            var round = run.UpcomingRound;
            var snapshot = Capture(ctx, run, policy.Name, round);
            if (SnapshotRounds.Contains(round))
            {
                snapshots[round] = snapshot;
            }

            var opponent = OpponentLadder.For(runSeed, round);
            var seedOffset = ArenaSeeds.Encounter(run.RunSeed, round);
            var duel = SideSwappedDuel.Sample(snapshot, opponent, fightSeeds, seedOffset);
            var won = duel.WinRate >= 0.5;
            run.ApplyMatchResult(won, opponent.PlayerId);
            var lifetime = LifetimeGold(run);
            fights.Add(new FightRecord(
                round,
                won,
                run.Gold,
                lifetime - run.Gold,
                opponent.PlayerId,
                duel.WinRate,
                snapshot.GoldSpent));
        }

        var endLifetime = LifetimeGold(run);
        return new RunResult(
            policy.Name,
            runSeed,
            run.Wins,
            run.Losses,
            run.IsVictory,
            run.IsRunOver && run.FightsPlayed <= 16,
            run.Gold,
            endLifetime,
            endLifetime - run.Gold,
            run.FightsPlayed,
            goldWentNegative || run.Gold < 0,
            fights,
            snapshots);
    }

    private static int StableHash(string value)
    {
        var hash = 23;
        foreach (var c in value)
        {
            hash = unchecked(hash * 31 + c);
        }

        return hash;
    }

    private static int LifetimeGold(ArenaRun run) =>
        ArenaRun.StartingGold + run.Wins * ArenaRun.WinGold + run.Losses * ArenaRun.LoseGold;

    private static BuildSnapshot Capture(GameContext ctx, ArenaRun run, string policy, int round)
    {
        var snapshot = BuildSnapshotFactory.ToSnapshot(
            ctx.PlayerPawn,
            "left",
            $"{policy}-r{round}-{run.RunSeed}",
            run.RunSeed,
            round);
        return snapshot with
        {
            GoldSpent = BuildGenerator.ComputeGoldSpent(snapshot),
            Round = round
        };
    }
}
