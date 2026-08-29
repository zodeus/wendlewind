namespace Wendlewind.Sim.Entities.Items.Medicinals;

/// <summary>
/// Handler for BoneCleanse - removes all body part modifiers from all bones in the body
/// and restores 25% bone health, regardless of which part is clicked.
/// </summary>
[UsedImplicitly]
public class BoneCleanseHandler : MedicinalHandler
{

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null) return false;

        // Find all bone parts in the entire body
        var boneParts = body.AllParts
            .Where(p => p.Substance == SubstanceType.Bone)
            .ToList();

        if (boneParts.Count == 0) return false;

        var anyEffect = false;

        foreach (var bonePart in boneParts)
        {
            // Remove all modifiers from this bone
            if (bonePart.Modifiers.Count > 0)
            {
                bonePart.Modifiers.Clear();
                anyEffect = true;
            }

            // Restore 25% bone health
            if (bonePart.HealthPercent < 1)
            {
                var healAmount = bonePart.MaxHitPoints * 0.25;
                bonePart.HitPoints = Math.Min(bonePart.MaxHitPoints, bonePart.HitPoints + healAmount);
                anyEffect = true;
            }
        }

        return anyEffect;
    }
}
