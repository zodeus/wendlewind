namespace Grafted.Sim.Entities.Items.Equipment;

public class BlessedIronCollarHandler : EquipmentHandler
{
    public const float NegativeNeckDrain = 0.1f;
    public const float NegativeNeckDrainPerTick = 0.00025f;
    public override bool OnPreDamageTaken(DamageRequest request, DamageResponse response)
    {
        if (request.TargetedPart.Type == BodyPartType.Neck)
        {
            var damageRecord = new DamageRecord(
                "Nothing", "Blessed Iron Collar", DamageType.Magic, request.TargetedPart, 0, request.TotalRawDamage
            );
            response.Damages.Add(damageRecord);
            return true;
        }
        return false;
    }

    public override void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
        base.Tick();
        bodyPart.HitPoints -= bodyPart.MaxHitPoints * NegativeNeckDrainPerTick;
        if (bodyPart.Skin != null )
        {
            bodyPart.Skin.HitPoints -= bodyPart.Skin.MaxHitPoints * NegativeNeckDrainPerTick;
        }
    }
}

