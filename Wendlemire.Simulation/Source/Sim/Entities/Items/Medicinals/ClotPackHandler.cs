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

    public override string GetEffectDescription(Item item) =>
        $"Packs the veins with a clot worth {BloodRestoreFraction:0%} of max blood.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Restores a quarter of maximum blood, instantly.",
        "Does not heal wounds — blood still leaks if the meat is open.",
        "Does nothing when the body is already full."
    ];
}
