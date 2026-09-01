namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class RhinoSkinHandler : EnchantmentHandler
{
    public RhinoSkinHandler(IRng rng)
    {
        Rng = rng;
    }

    private const double DamageMitigationBase = 0.1; // 10% of the hit refunded
    private const double DamageMitigationLevelFactor = 0.01; // 1% damage per level
    private int _level = 1;
    private double DamageMitigationFactor => DamageMitigationBase + ((_level - 1) * DamageMitigationLevelFactor);

    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord)
    {
        if (damageRecord.BodyParts.Any(r => r.BodyPart.Type == BodyPartType.Skin && r.WasDestroyed))
        {
            _level++;
        }

        if (bodyPart.Skin?.HealthPercent < .5f)
        {
            ApplyPartRegeneration(bodyPart, damageRecord);
        }

        if (bodyPart.Skin?.IsDestroyed == true) return;
        var damageMitigated = damageRecord.ActualAmount * DamageMitigationFactor;
        bodyPart.HitPoints += damageMitigated;
    }

    private void ApplyPartRegeneration(BodyPart bodyPart, DamageRecord damageRecord)
    {
        foreach (var modifier in Enchantment.ItemDef.EnchantmentProperties!.BodyPartModifiers)
        {
            // Add rhino skin to records
            damageRecord.BodyParts.First(p => Equals(p.BodyPart, bodyPart))
                .AppliedModifiers.Add(modifier.Def);

            bodyPart.ApplyBodyPartModifier(modifier, Enchantment.Label);
            foreach (var part in bodyPart.AllInternalParts)
            {
                part.ApplyBodyPartModifier(modifier, Enchantment.Label);
            }
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _level, "Level");
        base.ExposeData();
    }
}