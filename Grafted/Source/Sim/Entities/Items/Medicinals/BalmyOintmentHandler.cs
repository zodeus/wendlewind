namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BalmyOintmentHandler : MedicinalHandler {
    public override bool ApplyToPart(Item item, BodyPart part) {
        int duration = 1200;
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        foreach (BodyPart internalPart in part.AllInternalParts) {
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        }

        return true;
    }
}