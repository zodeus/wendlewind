namespace Wendlemire.Sim.Entities.Items.Equipment;

public class BlessedIronCollarHandler : EquipmentHandler
{
    public BlessedIronCollarHandler(IRng rng)
    {
        Rng = rng;
    }

    public const float DamagePerTick = 0.02f;
    public const float SoftTissueDamagePerTick = 0.006f;
    public override bool OnPreDamageTaken(DamageRequest request, DamageResponse response)
    {
        if (request.TargetedPart.Type == BodyPartType.Neck)
        {
            var damageRecord = new DamageRecord(
                "Nothing",
                "Blessed Iron Collar",
                DamageType.Magic,
                request.TargetedPart,
                0,
                request.TotalRawDamage,
                weaponMoniker: request.RawDamages.FirstOrDefault()?.Weapon.ItemDef.Moniker)
            {
                BlockingItemMoniker = Equipment.ItemDef.Moniker,
                BlockingItemLabel = Equipment.Label
            };
            response.Damages.Add(damageRecord);
            return true;
        }
        return false;
    }

    public override void Tick(Pawn pawn, BodyPart bodyPart)
    {
        bodyPart.HitPoints -= DamagePerTick;
        if (bodyPart.Skin != null)
        {
            bodyPart.Skin.HitPoints -= DamagePerTick;
        }
        bodyPart.Bones.ForEach(bone => bone.HitPoints -= SoftTissueDamagePerTick);
        bodyPart.Arteries.ForEach(artery => artery.HitPoints -= SoftTissueDamagePerTick);
    }
}

