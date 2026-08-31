using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Combat;

namespace Wendlewind.NetCode;

public static class DuelSimulator
{
    public static CombatResult Run(BuildSnapshot attacker, BuildSnapshot defender, int seed)
    {
        var duel = CombatReplay.RunDuel(
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

        var winnerId = duel.AttackerAlive ? attacker.PlayerId : defender.PlayerId;
        return new CombatResult
        {
            MatchId = Guid.NewGuid().ToString("N"),
            WinnerPlayerId = winnerId,
            Ticks = duel.Ticks,
            CauseOfDeath = duel.CauseOfDeath,
            DefenderPlayerId = defender.PlayerId,
            Defender = defender,
            EncounterSeed = duel.EncounterSeed
        };
    }
}
