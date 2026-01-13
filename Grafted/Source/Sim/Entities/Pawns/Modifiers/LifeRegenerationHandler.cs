namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class LifeRegenerationHandler : BodyPartModifier
{
    private const double HealthRegenerationPerTick = .01f;
    private const float ChanceToRegenerateDestroyedPart = 0.01f;

    public override void Tick()
    {
        if (BodyPart.HitPoints < 1 && Core.Random.Chance(ChanceToRegenerateDestroyedPart))
        {
            BodyPart.HitPoints = 1;
        }

        BodyPart.HitPoints += BodyPart.HitPoints * HealthRegenerationPerTick;
        base.Tick();
    }

    public override Widget? GetInfoPanel() => BuildInfoPanel(new InfoPanelData
    {
        Healing = HealthRegenerationPerTick * 100,
        HealingSuffix = "% health/tick",
        Lines = [new($"{ChanceToRegenerateDestroyedPart * 100:0.#}% chance to revive part", InfoColors.Info)]
    });
}