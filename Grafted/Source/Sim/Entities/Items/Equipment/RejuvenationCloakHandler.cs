namespace Grafted.Sim.Entities.Items.Equipment;

public class RejuvenationCloakHandler : EquipmentHandler
{
    public const float RejuvenationPerTick = 0.01f;

    public override void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
        base.Tick();
        var parts = pawn.Body?.AllParts ?? [];
        foreach (var part in parts)
        {
            if (part.IsSevered) { continue; }
            if (part.HitPoints <= 0) { continue; }
            if (part.HitPoints >= part.MaxHitPoints) { continue; }

            part.HitPoints += RejuvenationPerTick;
        }
    }
}
 