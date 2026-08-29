namespace Wendlewind.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BalmyOintmentHandler : MedicinalHandler
{
    public BalmyOintmentHandler(IRng rng)
    {
        Rng = rng;
    }

    private static readonly Color BalmColor = new(180, 200, 140);        // Soft green for soothing
    private static readonly Color PartColor = new(180, 150, 130);        // Flesh tone
    private static readonly Color InternalColor = new(140, 120, 110);    // Darker for internal
    private static readonly Color EffectColor = new(220, 200, 100);      // Golden glow effect
    private const double SoothingBalmPower = 1;

    public override bool ApplyToPart(Item item, BodyPart part)
    {
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
