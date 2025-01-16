using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BalmyOintmentHandler : MedicinalHandler
{
    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        }

        return true;
    }
}