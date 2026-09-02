namespace Wendlemire.Sim.Entities.Items.Medicinals;

/// <summary>
/// Handler for StrengthenBones - increases the max HP of all bones by 40%
/// and fully heals them, regardless of which part is clicked.
/// </summary>
[UsedImplicitly]
public class StrengthenBonesHandler : MedicinalHandler
{
    public StrengthenBonesHandler(IRng rng)
    {
        Rng = rng;
    }

    private const double MaxHpIncreasePercent = 0.40; // 40% increase


    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null) return false;

        // Find all bone parts in the entire body
        var boneParts = body.AllParts
            .Where(p => p.Substance == SubstanceType.Bone)
            .ToList();

        if (boneParts.Count == 0) return false;

        foreach (var bonePart in boneParts)
        {
            // Increase max HP by 40%
            var hpIncrease = bonePart.MaxHitPoints * MaxHpIncreasePercent;
            bonePart.MaxHitPoints += hpIncrease;

            // Fully heal the bone
            bonePart.HitPoints = bonePart.MaxHitPoints;
        }

        return true;
    }

    public override string GetEffectDescription(Item item) =>
        "Permanently densifies every bone: +40% max health, then a full heal.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Installs once at battle start and stays on for the fight.",
        "Raises max health on all bones by 40%, then fills them.",
        "Does not stack if you slot two."
    ];
}
