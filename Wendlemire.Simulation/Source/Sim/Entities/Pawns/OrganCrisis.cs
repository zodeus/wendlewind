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
