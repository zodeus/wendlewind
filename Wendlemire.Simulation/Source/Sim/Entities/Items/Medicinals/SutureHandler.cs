namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class SutureHandler : MedicinalHandler
{
    public SutureHandler(IRng rng)
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

    public override string GetEffectDescription(Item item) =>
        "Fully repairs one damaged artery.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Stitches the first damaged artery under the targeted limb.",
        "Does not heal flesh, bone, or organs.",
        "A dead artery will keep a limb from working until this lands."
    ];
}
