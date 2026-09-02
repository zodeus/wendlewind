namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class AntidoteHandler : MedicinalHandler
{
    public AntidoteHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null)
        {
            return false;
        }

        var any = false;
        foreach (var bodyPart in body.AllParts)
        {
            foreach (var modifier in bodyPart.Modifiers.ToList())
            {
                if (modifier.Def != Defs.BodyPartModifiers.Poison)
                {
                    continue;
                }

                modifier.IsExpired = true;
                bodyPart.Modifiers.Remove(modifier);
                any = true;
            }
        }

        return any;
    }

    public override string GetEffectDescription(Item item) =>
        "Burns poison out of every vein.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Purges Poison from the whole body at once.",
        "Does nothing if nothing is poisoned.",
        "Does not treat festering, necrosis, or other rot."
    ];
}
