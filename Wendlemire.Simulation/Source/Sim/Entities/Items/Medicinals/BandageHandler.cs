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

    public override string GetEffectDescription(Item item) =>
        "Fully heals the flesh and skin of one limb.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Wraps the targeted limb's meat and skin back together.",
        "Does not touch bone, arteries, or organs.",
        "Closing the flesh stops that part from bleeding."
    ];
}
