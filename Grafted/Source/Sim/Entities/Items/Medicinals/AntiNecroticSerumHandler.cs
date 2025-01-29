using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class AntiNecroticSerumHandler : MedicinalHandler
{
    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        if (part.HasModifier(Defs.BodyPartModifiers.Necrosis))
        {
            part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.NecrosisSerum, duration));
            return true;
        }

        return false;
    }
}