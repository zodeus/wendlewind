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

    public override string GetEffectDescription(Item item) =>
        "Fully heals every bone under one limb.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Pastes cracked bones on the targeted limb back to full.",
        "Does not heal flesh, skin, arteries, or organs.",
        "Only the bones on that limb — not the whole skeleton."
    ];
}
