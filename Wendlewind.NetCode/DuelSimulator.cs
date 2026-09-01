using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Combat;

namespace Wendlewind.NetCode;

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
            EncounterSeed = duel.Summary.EncounterSeed
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
}
