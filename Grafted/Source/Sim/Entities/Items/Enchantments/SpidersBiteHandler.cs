namespace Grafted.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SpidersBiteHandler : EnchantmentHandler
{
    public int Bites;

    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn target, DamageRecord damageRecord)
    {
        var randomPart = target.Body.AllExternalParts.RandomElement();
        if (randomPart.Skin is not { } skin) return;

        Bites++;
        foreach (var modifier in Enchantment.ItemDef.EnchantmentProperties!.BodyPartModifiers)
        {
            if (skin.ApplyBodyPartModifier(modifier))
            {
                damageRecord.SourceAfflictions.Add(new AfflictionRecord(randomPart, "Bitten"));
            }
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref Bites, "Bites");
        base.ExposeData();
    }
}