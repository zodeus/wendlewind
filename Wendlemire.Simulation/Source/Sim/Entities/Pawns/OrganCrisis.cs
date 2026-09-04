namespace Wendlemire.Sim.Entities.Pawns;

/// <summary>
/// Delayed death for guts that are not instant-vital: liver, intestines, or both kidneys.
/// Heart / brain / both-lungs still kill immediately via <see cref="Pawn.IsDeadFromPartFailure"/>.
/// </summary>
public sealed class OrganCrisis : IExposable
{
    private static readonly BodyPartType[] DelayedTypes =
    [
        BodyPartType.Liver,
        BodyPartType.Intestines,
        BodyPartType.Kidney
    ];

    private Dictionary<BodyPartType, int> _ticksInCrisis = new();

    public DeathRecord? Tick(PawnBody body)
    {
        var parts = body.AllParts;
        foreach (var type in DelayedTypes)
        {
            if (!TryCrisisLabel(parts, type, out var label))
            {
                _ticksInCrisis.Remove(type);
                continue;
            }

            var ticks = _ticksInCrisis.GetValueOrDefault(type) + 1;
            _ticksInCrisis[type] = ticks;
            if (ticks == 1)
            {
                body.NotifyOrganCrisisStarted(label);
            }

            if (ticks < CombatBalance.DelayedOrganFailureTicks)
            {
                continue;
            }

            return new DeathRecord
            {
                FailedOrgan = label,
                CauseOfDeath = $"{label} failed (systemic collapse)",
                KillingWeapon = "Organ failure",
                KillingManeuver = "Organ failure"
            };
        }

        return null;
    }

    public int TicksInCrisis(BodyPartType type) => _ticksInCrisis.GetValueOrDefault(type);

    public bool IsActive(BodyPartType type) => _ticksInCrisis.ContainsKey(type);

    public static bool IsDelayedType(BodyPartType type) =>
        type is BodyPartType.Liver or BodyPartType.Intestines or BodyPartType.Kidney;

    public static void CountType(PawnBody body, BodyPartType type, out int failed, out int total)
    {
        failed = 0;
        total = 0;
        var parts = body.AllParts;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Type != type)
            {
                continue;
            }

            total++;
            if (IsInCrisis(part))
            {
                failed++;
            }
        }
    }

    public bool IsPending(BodyPart part) =>
        IsDelayedType(part.Type) && IsInCrisis(part) && !IsActive(part.Type);

    public int TicksRemaining(BodyPartType type)
    {
        if (!_ticksInCrisis.TryGetValue(type, out var ticks))
        {
            return 0;
        }

        return Math.Max(0, CombatBalance.DelayedOrganFailureTicks - ticks);
    }

    public bool TryGetImminent(PawnBody body, out OrganCrisisStatus status)
    {
        status = default;
        var bestRemaining = int.MaxValue;
        var found = false;
        var parts = body.AllParts;
        foreach (var type in DelayedTypes)
        {
            if (!_ticksInCrisis.TryGetValue(type, out var ticks))
            {
                continue;
            }

            if (!TryCrisisLabel(parts, type, out var label))
            {
                continue;
            }

            var remaining = Math.Max(0, CombatBalance.DelayedOrganFailureTicks - ticks);
            if (remaining >= bestRemaining)
            {
                continue;
            }

            bestRemaining = remaining;
            found = true;
            status = new OrganCrisisStatus(label, type, ticks, remaining);
        }

        return found;
    }

    private static bool TryCrisisLabel(IReadOnlyList<BodyPart> parts, BodyPartType type, out string label)
    {
        label = "";
        var found = false;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Type != type)
            {
                continue;
            }

            if (!IsInCrisis(part))
            {
                return false;
            }

            if (!found)
            {
                label = part.Label;
                found = true;
            }
        }

        return found;
    }

    public static bool IsInCrisis(BodyPart part)
    {
        if (part.IsDestroyed)
        {
            return true;
        }

        if (part.HealthPercent > CombatBalance.DelayedOrganFesterHealth)
        {
            return false;
        }

        return part.HasModifier(Defs.BodyPartModifiers.Festering)
            || part.HasModifier(Defs.BodyPartModifiers.Poison)
            || part.HasModifier(Defs.BodyPartModifiers.Necrosis);
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _ticksInCrisis!, "TicksInCrisis", LookMode.Value, LookMode.Value);
        _ticksInCrisis ??= new Dictionary<BodyPartType, int>();
    }
}

public readonly struct OrganCrisisStatus
{
    public OrganCrisisStatus(string label, BodyPartType type, int ticks, int ticksRemaining)
    {
        Label = label;
        Type = type;
        Ticks = ticks;
        TicksRemaining = ticksRemaining;
    }

    public string Label { get; }
    public BodyPartType Type { get; }
    public int Ticks { get; }
    public int TicksRemaining { get; }
    public float Progress => Math.Clamp(Ticks / (float)CombatBalance.DelayedOrganFailureTicks, 0f, 1f);
    public float RemainingSeconds => TicksRemaining / (float)GameContext.TicksPerSecond;
}
