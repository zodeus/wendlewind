namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RhinoRestorationHandler : BodyPartModifier
{
    private const double HealthPercentRestoredPerTick = .005;

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.HitPoints < 1)
        {
            BodyPart.HitPoints = 1;
        }

        BodyPart.HitPoints += BodyPart.HitPoints * HealthPercentRestoredPerTick;
    }

    public override bool ApplyToPart(BodyPart part)
    {
        part.TryAddModifier(this);

        return true;
    }
}