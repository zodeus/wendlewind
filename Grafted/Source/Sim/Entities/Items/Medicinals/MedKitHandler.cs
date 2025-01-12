namespace Grafted.Sim.Entities.Items.Medicinals;
[UsedImplicitly]
public class MedKitHandler : MedicinalHandler {
    public override bool ApplyToPart(Item item, BodyPart part) {
        if (part.HealthPercent >= 1 && part.InternalParts.Any(p => p.HealthPercent < 1) == false) {
            return false;
        }

        part.HitPoints = part.MaxHitPoints;
        foreach (BodyPart internalPart in part.InternalParts) {
            internalPart.HitPoints = internalPart.MaxHitPoints;
        }

        return true;
    }
}