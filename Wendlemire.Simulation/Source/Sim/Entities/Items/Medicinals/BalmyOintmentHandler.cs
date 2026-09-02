namespace Wendlemire.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BalmyOintmentHandler : MedicinalHandler
{
    public BalmyOintmentHandler(IRng rng)
    {
        Rng = rng;
    }

    private const double SoothingBalmPower = 1;

    public override string GetEffectDescription(Item item) =>
        "Puts out burning and acid, then keeps the meat soothed.";

    public override IReadOnlyList<string> GetHowItWorks(Item item)
    {
        var seconds = item.ItemDef.MedicinalProperties!.DurationInTicks / (float)GameContext.TicksPerSecond;
        return
        [
            "Clears Burning and Acid on the targeted limb and its internals.",
            $"Leaves Soothing Balm for {seconds:0.#}s, which keeps wiping those burns.",
            "Does not restore hit points."
        ];
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        if (!NeedsBalm(part) && part.AllInternalParts.All(p => !NeedsBalm(p)))
        {
            return false;
        }

        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        part.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.SoothingBalm, duration, 1));
        RemoveBurningAndAcid(part);
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(Context.Factory.CreateModifier(Defs.BodyPartModifiers.SoothingBalm, duration, SoothingBalmPower));
            RemoveBurningAndAcid(internalPart);
        }

        return true;
    }

    private static bool NeedsBalm(BodyPart part)
    {
        return HasBurningOrAcid(part) || part.HasModifier(Defs.BodyPartModifiers.SoothingBalm) == false;
    }

    private static bool HasBurningOrAcid(BodyPart part)
    {
        return part.HasModifier(Defs.BodyPartModifiers.Burning)
               || part.HasModifier(Defs.BodyPartModifiers.Acid);
    }

    private void RemoveBurningAndAcid(BodyPart part)
    {
        foreach (var modifier in part.Modifiers.ToList())
        {
            if (modifier.Def == Defs.BodyPartModifiers.Burning || modifier.Def == Defs.BodyPartModifiers.Acid)
            {
                modifier.IsExpired = true;
                part.Modifiers.Remove(modifier);
            }
        }
    }
}
