namespace Wendlemire.Sim.Entities.Items.Medicinals;

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

    public override string GetEffectDescription(Item item) =>
        "Fully reconstructs one limb — bone, flesh, skin, and organs.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Heals the targeted part and every internal under it.",
        "One charge, one limb. Does not spread through sockets.",
        "Does not seal a severed stump."
    ];
}
