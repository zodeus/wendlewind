namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class BloodBathHandler : EnchantmentHandler
{
    public BloodBathHandler(IRng rng)
    {
        Rng = rng;
    }

    private const float BloodScaleFactor = 0.02f;

    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord)
    {
        target.Body.BloodAmount += target.Body.MaxBlood * BloodScaleFactor * GetMagic(target);
        damageRecord.ReflectedEffects.Add(new ReflectedStatusEffect(target, Enchantment.ItemDef, $"Blood bath applied to {bodyPart.Label}", HostItemMoniker(bodyPart)));
    }
}