namespace Grafted.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SpidersBiteHandler : EnchantmentHandler
{
    public int Bites;

    // Armor handler
    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn target, DamageRecord damageRecord)
    {
        var randomPart = target.Body.AllExternalParts.RandomElement();
        Bites++;
        foreach (var modifier in Enchantment.ItemDef.EnchantmentProperties!.BodyPartModifiers)
        {
            var modRecord = new BodyPartModifierRecord
            {
                Def = modifier.Def,
                DurationInTicks = modifier.DurationInTicks,
                Chance = RangeFloat.One,
                Power = modifier.Power
            };
            if (randomPart.ApplyBodyPartModifier(modRecord, Enchantment.Label))
            {
                damageRecord.DamageStatusEffects.Add(
                    new DamageStatusEffect(target, Enchantment.ItemDef, $"/c[{TC.BodyPart}]{randomPart.Label} /c[{TC.Default}]was bitten by /c[{TC.BrightBlue}]{Enchantment.Label}")
                );
            }
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref Bites, "Bites");
        base.ExposeData();
    }
}