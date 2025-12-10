namespace Grafted.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class BloodBathHandler : EnchantmentHandler
{
    private const float BloodScaleFactor = 0.05f;

    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord)
    {
        Log.Info($"Applying blood bath enchantment to target {target.Label}");
        target.Body.BloodAmount += target.Body.MaxBlood * BloodScaleFactor;
        damageRecord.DamageStatusEffects.Add(new DamageStatusEffect(target, Enchantment.ItemDef, $"Blood bath applied to {bodyPart.Label}"));
    }
}