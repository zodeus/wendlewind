namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RhinoRestorationHandler : BodyPartModifier
{
    private const double HealthRestoredPerTick = .02;
    private const double SkinHealthRestoredPerTick = .03;

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.HitPoints < 1)
        {
            BodyPart.HitPoints = 1;
        }

        var health = BodyPart.Type == BodyPartType.Skin ? SkinHealthRestoredPerTick : HealthRestoredPerTick;
        BodyPart.HitPoints += health;
    }

    public override bool ApplyToPart(BodyPart part)
    {
        part.TryAddModifier(this);

        return true;
    }

    public override Widget? GetInfoPanel()
    {
        var isSkin = BodyPart?.Type == BodyPartType.Skin;
        var healRate = isSkin ? SkinHealthRestoredPerTick : HealthRestoredPerTick;

        return BuildInfoPanel(new InfoPanelData
        {
            Healing = healRate,
            HealingSuffix = isSkin ? "health/tick (skin)" : "health/tick",
            Lines = [new("Prevents part destruction", InfoColors.Info)]
        });
    }
}