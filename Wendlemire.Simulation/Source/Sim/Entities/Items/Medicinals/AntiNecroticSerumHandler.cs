namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class AntiNecroticSerumHandler : MedicinalHandler
{
    public AntiNecroticSerumHandler(IRng rng)
    {
        Rng = rng;
    }


    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        if (part.HasModifier(Defs.BodyPartModifiers.Necrosis) && part.HasModifier(Defs.BodyPartModifiers.NecrosisSerum) == false)
        {
            part.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.NecrosisSerum, duration, 1));
            return true;
        }

        return false;
    }

    public override string GetEffectDescription(Item item) =>
        "Starts a timed treatment that cures necrosis when it finishes.";

    public override IReadOnlyList<string> GetHowItWorks(Item item)
    {
        var seconds = item.ItemDef.MedicinalProperties!.DurationInTicks / (float)GameContext.TicksPerSecond;
        return
        [
            "Only works on a part that already has necrosis.",
            $"Applies Necrosis Serum for {seconds:0.#}s. When it expires, the necrosis goes with it.",
            "Does not heal the hole the rot already chewed."
        ];
    }
}
