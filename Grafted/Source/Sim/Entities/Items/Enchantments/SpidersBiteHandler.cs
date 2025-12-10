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
        Log.Info($"SpidersBiteHandler: Bites: {Bites}");
        foreach (var modifier in Enchantment.ItemDef.EnchantmentProperties!.BodyPartModifiers)
        {
            Log.Info($"SpidersBiteHandler: Applying modifier: {modifier.Def.Label}");
            if (randomPart.ApplyBodyPartModifier(modifier, Enchantment.Label))
            {
                Log.Info($"SpidersBiteHandler: Modifier applied: {modifier.Def.Label}");
                damageRecord.DamageStatusEffects.Add(new DamageStatusEffect(target, Enchantment.ItemDef, $"{randomPart.Label} was bitten by {Enchantment.Label}"));
            }
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref Bites, "Bites");
        base.ExposeData();
    }
}