namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class RhinoSkinHandler : EnchantmentHandler
{
    public RhinoSkinHandler(IRng rng)
    {
        Rng = rng;
    }

    private const double DamageMitigationBase = 0.08; // 8% of the hit refunded
    private const double DamageMitigationLevelFactor = 0.01; // 1% damage per level
    private int _level = 1;
    public int Level => _level;
    private double DamageMitigationFactor => DamageMitigationBase + ((_level - 1) * DamageMitigationLevelFactor);

    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord)
    {
        var magic = GetMagic(target);
        if (damageRecord.BodyParts.Any(r => r.BodyPart.Type == BodyPartType.Skin && r.WasDestroyed))
        {
            _level++;
        }

        if (bodyPart.Skin?.HealthPercent < .5f)
        {
            ApplyPartRegeneration(bodyPart, damageRecord, magic);
        }

        if (bodyPart.Skin?.IsDestroyed == true) return;
        var damageMitigated = damageRecord.ActualAmount * DamageMitigationFactor * magic;
        bodyPart.HitPoints += damageMitigated;
    }

    private void ApplyPartRegeneration(BodyPart bodyPart, DamageRecord damageRecord, float magic)
    {
        var properties = Enchantment.ItemDef.EnchantmentProperties!;
        foreach (var modifier in properties.BodyPartModifiers)
        {
            var scaled = properties.ScaleRecord(modifier, magic);
            // Add rhino skin to records
            damageRecord.BodyParts.First(p => Equals(p.BodyPart, bodyPart))
                .AppliedModifiers.Add(scaled.Def);

            bodyPart.ApplyBodyPartModifier(scaled, Enchantment.Label);
            foreach (var part in bodyPart.AllInternalParts)
            {
                part.ApplyBodyPartModifier(scaled, Enchantment.Label);
            }
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _level, "Level");
        base.ExposeData();
    }
}