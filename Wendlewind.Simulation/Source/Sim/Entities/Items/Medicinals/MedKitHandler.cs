namespace Wendlewind.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MedKitHandler : MedicinalHandler
{
    public MedKitHandler(IRng rng)
    {
        Rng = rng;
    }


    public override bool ApplyToPart(Item item, BodyPart part)
    {
        if (part.HealthPercent >= 1 && part.AllInternalParts.Any(p => p.HealthPercent < 1) == false)
        {
            return false;
        }

        part.HitPoints = part.MaxHitPoints;
        foreach (BodyPart internalPart in part.AllInternalParts)
        {
            internalPart.HitPoints = internalPart.MaxHitPoints;
        }

        return true;
    }
}
