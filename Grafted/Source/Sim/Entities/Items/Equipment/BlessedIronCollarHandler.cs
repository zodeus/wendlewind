namespace Grafted.Sim.Entities.Items.Equipment;

public class BlessedIronCollarHandler : EquipmentHandler
{
    public const float NegativeNeckDrain = 0.1f;
    public const float NegativeNeckDrainPerTick = 0.0002f;
    public override bool OnPreDamageTaken(DamageRequest request, DamageResponse response)
    {
        Log.Info($"Blessed Iron Collar: OnPreDamageTaken: {request.TargetedPart.Label}");
        if (request.TargetedPart.Type == BodyPartType.Neck)
        {
            Log.Info($"Blessed Iron Collar blocked {request.TotalRawDamage} damage");
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

