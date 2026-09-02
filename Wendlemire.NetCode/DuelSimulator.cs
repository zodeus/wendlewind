using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Combat;

namespace Wendlemire.NetCode;

public sealed record DuelSimulation
{
    public required CombatResult Result { get; init; }
    public required CombatLogEvent[] Log { get; init; }
    public required FightAnalytics Analytics { get; init; }
}

public static class DuelSimulator
{
    public static CombatResult Run(BuildSnapshot attacker, BuildSnapshot defender, int seed) =>
        Simulate(attacker, defender, seed).Result;

    public static DuelSimulation Simulate(BuildSnapshot attacker, BuildSnapshot defender, int seed)
    {
        var duel = CombatReplay.RunDuelWithLog(
            seed,
            (context, player, enemy) =>
            {
                BuildSnapshotFactory.Apply(player, attacker);
                BuildSnapshotFactory.Apply(enemy, defender);
            },
            attacker.PawnDefMoniker,
            attacker.PawnName ?? "Attacker",
            defender.PawnDefMoniker,
            defender.PawnName ?? "Defender");

        var winnerId = duel.Summary.AttackerAlive ? attacker.PlayerId : defender.PlayerId;
        var result = new CombatResult
        {
            MatchId = Guid.NewGuid().ToString("N"),
            WinnerPlayerId = winnerId,
            Ticks = duel.Summary.Ticks,
            CauseOfDeath = duel.Summary.CauseOfDeath,
            DefenderPlayerId = defender.PlayerId,
            Defender = defender,
            EncounterSeed = duel.Summary.EncounterSeed,
            Version = GameVersion.Current
        };
        var analytics = CombatAnalytics.From(
            duel.Log,
            duel.Summary.Ticks,
            duel.AttackerPawnId,
            duel.DefenderPawnId,
            duel.KillingWeapon,
            duel.KillingManeuver,
            duel.AttackerBloodPercent,
            duel.DefenderBloodPercent);
        return new DuelSimulation
        {
            Result = result,
            Log = duel.Log,
            Analytics = analytics
        };
    }

    public static string DescribeMismatch(
        CombatResult server,
        bool localWon,
        string localPlayerId,
        int localTicks,
        string? localCause)
    {
        var localWinner = localWon ? localPlayerId : server.DefenderPlayerId ?? "?";
        return
            $"Arena re-sim disagreed with server. MatchId={server.MatchId} Seed={server.EncounterSeed} " +
            $"ServerWinner={server.WinnerPlayerId} ServerTicks={server.Ticks} ServerCause={server.CauseOfDeath} " +
            $"LocalWinner={localWinner} LocalTicks={localTicks} LocalCause={localCause} " +
            $"ServerVersion={server.Version} LocalVersion={GameVersion.Current}";
    }
}
