namespace Wendlewind.Sim.Combat;

public sealed record FightSideStats
{
    public double DamageDealt { get; init; }
    public double Healing { get; init; }
    public double DotTaken { get; init; }
    public int Hits { get; init; }
    public int Misses { get; init; }
    public int Dodges { get; init; }
    public int Blocks { get; init; }
    public int PotionUses { get; init; }
    public int MedicalUses { get; init; }
    public int Severs { get; init; }
    public float BloodPercent { get; init; }
    public double DamagePerSecond { get; init; }
}

public sealed record FightAnalytics
{
    public int DurationTicks { get; init; }
    public double DurationSeconds { get; init; }
    public bool InTargetBand { get; init; }
    public FightSideStats Attacker { get; init; } = new();
    public FightSideStats Defender { get; init; } = new();
    public string? KillingWeapon { get; init; }
    public string? KillingManeuver { get; init; }
    public int? FirstDamageTick { get; init; }
    public int? LastDamageTick { get; init; }
}

public static class CombatAnalytics
{
    public const double TargetMinSeconds = 15;
    public const double TargetMaxSeconds = 25;

    public static bool IsInTargetBand(double seconds) =>
        seconds >= TargetMinSeconds && seconds <= TargetMaxSeconds;

    public static double TicksToSeconds(int ticks) =>
        ticks / (double)GameContext.TicksPerSecond;

    public static FightAnalytics From(
        IReadOnlyList<CombatLogEvent> log,
        int ticks,
        int attackerPawnId,
        int defenderPawnId,
        string? killingWeapon = null,
        string? killingManeuver = null,
        float attackerBloodPercent = 0,
        float defenderBloodPercent = 0)
    {
        var attacker = new SideAccumulator();
        var defender = new SideAccumulator();
        int? firstDamageTick = null;
        int? lastDamageTick = null;

        foreach (var ev in log)
        {
            var subject = SideFor(ev.SubjectPawnId, attackerPawnId, defenderPawnId, attacker, defender);
            var source = ev.SourcePawnId is int sourceId
                ? SideFor(sourceId, attackerPawnId, defenderPawnId, attacker, defender)
                : null;

            CountSevers(ev, subject, attackerPawnId, defenderPawnId, attacker, defender);
            switch (ev.Kind)
            {
                case CombatEventKind.Damage:
                    if (source != null)
                    {
                        source.DamageDealt += ev.Amount;
                        source.Hits++;
                    }

                    if (ev.Blocked > 0 && subject != null)
                    {
                        subject.Blocks++;
                    }

                    firstDamageTick ??= ev.Tick;
                    lastDamageTick = ev.Tick;
                    break;
                case CombatEventKind.Block:
                    if (subject != null)
                    {
                        subject.Blocks++;
                    }

                    break;
                case CombatEventKind.Miss:
                    if (source != null)
                    {
                        source.Misses++;
                    }

                    break;
                case CombatEventKind.Dodge:
                    if (subject != null)
                    {
                        subject.Dodges++;
                    }

                    break;
                case CombatEventKind.Heal:
                    if (subject != null)
                    {
                        subject.Healing += ev.Amount;
                    }

                    break;
                case CombatEventKind.DamageOverTime:
                    if (subject != null)
                    {
                        subject.DotTaken += ev.Amount;
                    }

                    break;
                case CombatEventKind.PotionUsed:
                    if (subject != null)
                    {
                        subject.PotionUses++;
                    }

                    break;
                case CombatEventKind.MedicalUsed:
                    if (subject != null)
                    {
                        subject.MedicalUses++;
                    }

                    break;
            }
        }

        var seconds = TicksToSeconds(ticks);
        return new FightAnalytics
        {
            DurationTicks = ticks,
            DurationSeconds = seconds,
            InTargetBand = IsInTargetBand(seconds),
            Attacker = attacker.ToStats(seconds, attackerBloodPercent),
            Defender = defender.ToStats(seconds, defenderBloodPercent),
            KillingWeapon = killingWeapon,
            KillingManeuver = killingManeuver,
            FirstDamageTick = firstDamageTick,
            LastDamageTick = lastDamageTick
        };
    }

    private static SideAccumulator? SideFor(
        int pawnId,
        int attackerPawnId,
        int defenderPawnId,
        SideAccumulator attacker,
        SideAccumulator defender)
    {
        if (pawnId == attackerPawnId)
        {
            return attacker;
        }

        if (pawnId == defenderPawnId)
        {
            return defender;
        }

        return null;
    }

    private static void CountSevers(
        CombatLogEvent ev,
        SideAccumulator? subject,
        int attackerPawnId,
        int defenderPawnId,
        SideAccumulator attacker,
        SideAccumulator defender)
    {
        if (ev.Kind == CombatEventKind.PartSevered && subject != null)
        {
            subject.Severs++;
        }

        foreach (var sub in ev.SubEffects)
        {
            if (sub.Kind != CombatEventKind.PartSevered)
            {
                continue;
            }

            var subSubject = SideFor(sub.SubjectPawnId, attackerPawnId, defenderPawnId, attacker, defender);
            if (subSubject != null)
            {
                subSubject.Severs++;
            }
        }
    }

    private sealed class SideAccumulator
    {
        public double DamageDealt;
        public double Healing;
        public double DotTaken;
        public int Hits;
        public int Misses;
        public int Dodges;
        public int Blocks;
        public int PotionUses;
        public int MedicalUses;
        public int Severs;

        public FightSideStats ToStats(double seconds, float bloodPercent) => new()
        {
            DamageDealt = DamageDealt,
            Healing = Healing,
            DotTaken = DotTaken,
            Hits = Hits,
            Misses = Misses,
            Dodges = Dodges,
            Blocks = Blocks,
            PotionUses = PotionUses,
            MedicalUses = MedicalUses,
            Severs = Severs,
            BloodPercent = bloodPercent,
            DamagePerSecond = seconds > 0 ? DamageDealt / seconds : 0
        };
    }
}
