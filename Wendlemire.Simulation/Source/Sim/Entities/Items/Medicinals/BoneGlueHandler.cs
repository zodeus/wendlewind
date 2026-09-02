namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BoneGlueHandler : MedicinalHandler
{
    public BoneGlueHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var any = false;
        if (IsDamagedBone(part))
        {
            part.HitPoints = part.MaxHitPoints;
            any = true;
        }

        foreach (var internalPart in part.AllInternalParts)
        {
            if (IsDamagedBone(internalPart))
            {
                internalPart.HitPoints = internalPart.MaxHitPoints;
                any = true;
            }
        }

        return any;
    }

    private static bool IsDamagedBone(BodyPart part) =>
        part.Substance == SubstanceType.Bone && part.HealthPercent < 1;
}
