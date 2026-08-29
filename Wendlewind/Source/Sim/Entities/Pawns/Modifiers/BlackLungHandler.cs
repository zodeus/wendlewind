namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

/// <summary>
/// Handler for BlackLung modifier - damages lungs over time, applied via BlackenedSmoke potion.
/// Damage phases: Initial irritation → Acute inflammation → Brief recovery → Sustained damage
/// </summary>
[UsedImplicitly]
public class BlackLungHandler : BodyPartModifier
{
    // Damage phases (tick thresholds and damage rates)
    private static readonly (int EndTick, double DamageRate)[] DamagePhases =
    [
        (100, 0.1),   // Phase 1: Initial irritation (30 ticks, light damage)
        (300, 0.4),   // Phase 2: Acute inflammation (30 ticks, heavy damage)
        (400, 0.05),   // Phase 3: Brief recovery (20 ticks, minimal damage)
        (500, 0.2)   // Phase 4: Sustained damage (25 ticks, heavy damage)
    ];

    private double GetCurrentDamageRate()
    {
        var cycleTick = Ticks % DamagePhases[^1].EndTick;
        foreach (var (endTick, damageRate) in DamagePhases)
        {
            if (cycleTick < endTick)
                return damageRate;
        }
        return DamagePhases[^1].DamageRate;
    }

    public override void Tick()
    {
        BodyPart.HitPoints -= GetCurrentDamageRate();
        CheckIfLostVitalPart();
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        // Find all lungs and apply to them
        var lungs = part.Body?.AllParts.Where(p => p?.Type == BodyPartType.Lung).ToList();
        if (lungs == null || lungs.Count == 0)
        {
            Log.Warning($"No lungs found while applying body part modifier {Def.Moniker}");
            return false;
        }

        foreach (var lung in lungs)
        {
            lung.TryAddModifier(BodyPartModifierGenerator.Generate(Def, DurationInTicks, Power));
        }

        return true;
    }

    public override Widget? GetInfoPanel()
    {
        var cycleTick = Ticks % DamagePhases[^1].EndTick;
        var phaseName = cycleTick < DamagePhases[0].EndTick ? "Initial Irritation"
            : cycleTick < DamagePhases[1].EndTick ? "Acute Inflammation"
            : cycleTick < DamagePhases[2].EndTick ? "Brief Recovery"
            : "Sustained Damage";

        return BuildInfoPanel(new InfoPanelData
        {
            Damage = GetCurrentDamageRate(),
            DamageColor = new Color(80, 80, 80),
            Lines =
            [
                new($"Phase: {phaseName}", new Color(120, 100, 120)),
                new("Targets lungs specifically", new Color(150, 130, 150))
            ]
        });
    }
}
