namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class ClotPackHandler : MedicinalHandler
{
    public const float BloodRestoreFraction = 0.25f;

    public ClotPackHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null || body.BloodPercent >= 1)
        {
            return false;
        }

        body.BloodAmount += body.MaxBlood * BloodRestoreFraction;
        return true;
    }
}
