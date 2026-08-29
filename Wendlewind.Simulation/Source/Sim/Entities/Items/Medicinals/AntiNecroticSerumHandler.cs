namespace Wendlewind.Sim.Entities.Items.Medicinals;

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
}
