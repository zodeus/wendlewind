namespace Wendlemire.Sim.Entities.Items.Medicinals;

/// <summary>
/// Handler for Cyberveins - triples the max HP of all arteries
/// and fully heals them, regardless of which part is clicked.
/// </summary>
[UsedImplicitly]
public class CyberveinsHandler : MedicinalHandler
{
    public CyberveinsHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null) return false;

        var arteries = body.AllParts
            .Where(p => p.Type == BodyPartType.Artery)
            .ToList();

        if (arteries.Count == 0) return false;

        foreach (var artery in arteries)
        {
            artery.MaxHitPoints *= 3;
            artery.HitPoints = artery.MaxHitPoints;
        }

        return true;
    }

    public override string GetEffectDescription(Item item) =>
        "Permanently triples the durability of every artery.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Installs once at battle start and stays on for the fight.",
        "Triples max health on all arteries, then fills them.",
        "Does not stack if you slot two."
    ];
}
