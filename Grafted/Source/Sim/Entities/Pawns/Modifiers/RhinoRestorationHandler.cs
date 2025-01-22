namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RhinoRestorationHandler : BodyPartModifier
{
    private const double HealthPercentRestoredPerTick = .005;

    public override void Tick()
    {
        if (BodyPart.HitPoints < 1)
        {
            BodyPart.HitPoints = 1;
        }

        BodyPart.HitPoints += BodyPart.HitPoints * HealthPercentRestoredPerTick;

        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        part.TryAddModifier(this);

        return true;
    }


    public override void Expired()
    {
    }
}