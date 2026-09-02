namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BandageHandler : MedicinalHandler
{
    public BandageHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var any = false;
        if (part.Substance == SubstanceType.Flesh && part.HealthPercent < 1)
        {
            part.HitPoints = part.MaxHitPoints;
            any = true;
        }

        foreach (var internalPart in part.AllInternalParts)
        {
            if (internalPart.Type == BodyPartType.Skin && internalPart.HealthPercent < 1)
            {
                internalPart.HitPoints = internalPart.MaxHitPoints;
                any = true;
            }
        }

        return any;
    }
}
