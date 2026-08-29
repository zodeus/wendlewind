namespace Wendlewind.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class ArterialThreadsHandler : MedicinalHandler
{
    public ArterialThreadsHandler(IRng rng)
    {
        Rng = rng;
    }


    public override bool ApplyToPart(Item item, BodyPart part)
    {
        foreach (var internalPart in part.InternalParts)
        {
            if (internalPart.Type == BodyPartType.Artery && internalPart.HealthPercent < 1)
            {
                internalPart.HitPoints = internalPart.MaxHitPoints;
                return true;
            }
        }

        return false;
    }
}
