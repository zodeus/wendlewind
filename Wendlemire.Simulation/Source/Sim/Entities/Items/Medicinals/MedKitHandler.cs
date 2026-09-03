namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MedKitHandler : MedicinalHandler
{
    public MedKitHandler(IRng rng)
    {
        Rng = rng;
    }


    public const double OrganMissingHeal = 0.35;

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        if (part.HealthPercent >= 1 && part.AllInternalParts.Any(p => p.HealthPercent < 1) == false)
        {
            return false;
        }

        RestoreStructure(part);
        foreach (BodyPart internalPart in part.AllInternalParts)
        {
            RestoreStructure(internalPart);
        }

        return true;
    }

    private static void RestoreStructure(BodyPart part)
    {
        if (part.IsOrgan)
        {
            var missing = part.MaxHitPoints - part.HitPoints;
            if (missing > 0)
            {
                part.HitPoints += missing * OrganMissingHeal;
            }

            return;
        }

        part.HitPoints = part.MaxHitPoints;
    }

    public override string GetEffectDescription(Item item) =>
        "Reconstructs one limb — bone, flesh, and skin. Organs only recover a portion.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Fully heals the targeted part and its non-organ internals.",
        "Organs regain 35% of missing health — a kit cannot reset the heart.",
        "One charge, one limb. Does not spread through sockets or seal a severed stump."
    ];
}
