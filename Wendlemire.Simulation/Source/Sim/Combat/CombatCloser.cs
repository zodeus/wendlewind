namespace Wendlemire.Sim.Combat;

/// <summary>
/// Late-fight fuse: escalating blood drain after the 30–60s target band, then a hard resolve.
/// </summary>
public static class CombatCloser
{
    public const int DefaultStartSeconds = 80;
    public const int DefaultHardResolveSeconds = 120;
    public const float StartBloodPerTick = 1f;
    public const float EscalationPerSecond = 0.15f;
    public const string StartedMessage = "The wasting sets in";
    public const string CauseOfDeath = "The wasting";

    public static int StartTicks { get; private set; } = DefaultStartSeconds * GameContext.TicksPerSecond;
    public static int HardResolveTicks { get; private set; } = DefaultHardResolveSeconds * GameContext.TicksPerSecond;

    public static float BloodDrainPerTick(int encounterTicks)
    {
        if (encounterTicks < StartTicks)
        {
            return 0f;
        }

        var secondsAfterStart = (encounterTicks - StartTicks) / (float)GameContext.TicksPerSecond;
        return StartBloodPerTick + EscalationPerSecond * secondsAfterStart;
    }

    public static bool IsActive(int encounterTicks) => encounterTicks >= StartTicks;

    public static bool ShouldHardResolve(int encounterTicks) => encounterTicks >= HardResolveTicks;

    /// <summary>
    /// Lower blood percent loses; then fewer functional vitals. Tie goes to the defender
    /// so the attacker (player) wins.
    /// </summary>
    public static Pawn PickLoser(Pawn attacker, Pawn defender)
    {
        var blood = attacker.Body.BloodPercent.CompareTo(defender.Body.BloodPercent);
        if (blood < 0)
        {
            return attacker;
        }

        if (blood > 0)
        {
            return defender;
        }

        var vitals = CountFunctionalVitals(attacker).CompareTo(CountFunctionalVitals(defender));
        if (vitals < 0)
        {
            return attacker;
        }

        return defender;
    }

    public static int CountFunctionalVitals(Pawn pawn)
    {
        var count = 0;
        var parts = pawn.Body.AllParts;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.IsVital && part.IsFunctional)
            {
                count++;
            }
        }

        return count;
    }

    public static void OverrideTimingForTests(int startTicks, int hardResolveTicks)
    {
        StartTicks = startTicks;
        HardResolveTicks = hardResolveTicks;
    }

    public static void ResetTiming()
    {
        StartTicks = DefaultStartSeconds * GameContext.TicksPerSecond;
        HardResolveTicks = DefaultHardResolveSeconds * GameContext.TicksPerSecond;
    }
}
