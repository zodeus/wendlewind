namespace Grafted.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class BloodBathHandler : EnchantmentHandler
{
    private const float BloodScaleFactor = 0.05f;

    public override void HandlePawnTakeDamageEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord)
    {
        target.Body.BloodAmount += target.Body.MaxBlood * BloodScaleFactor;
    }
}