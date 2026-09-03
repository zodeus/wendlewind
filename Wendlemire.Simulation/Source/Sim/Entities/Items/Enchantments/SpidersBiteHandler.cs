namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SpidersBiteHandler : EnchantmentHandler
{
    public SpidersBiteHandler(IRng rng)
    {
        Rng = rng;
    }

    private int _bites;

    // Armor handler
    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn target, DamageRecord damageRecord)
    {
        var randomPart = target.Body.AllExternalParts.RandomElement(Context.Rng);
        _bites++;
        var properties = Enchantment.ItemDef.EnchantmentProperties!;
        var magic = GetMagic(pawn);
        var host = HostEquipment(pawn);
        var rhinoPair = ItemSynergies.HostHas(host, Defs.Items.RhinoSkin);
        var rhinoChance = rhinoPair ? ItemSynergies.BiteChanceFromRhino(ItemSynergies.RhinoLevel(host)) : 1f;
        foreach (var modifier in properties.BodyPartModifiers)
        {
            var scaled = properties.ScaleRecord(modifier, magic);
            var modRecord = new BodyPartModifierRecord
            {
                Def = scaled.Def,
                DurationInTicks = scaled.DurationInTicks,
                Chance = scaled.Chance * rhinoChance,
                Power = scaled.Power
            };
            if (randomPart.ApplyBodyPartModifier(modRecord, Enchantment.Label))
            {
                damageRecord.ReflectedEffects.Add(
                    new ReflectedStatusEffect(target, Enchantment.ItemDef, $"/c[{TC.BodyPart}]{randomPart.Label} /c[{TC.Default}]was bitten by /c[{TC.BrightBlue}]{Enchantment.Label} ", HostItemMoniker(bodyPart))
                );
            }
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _bites, "Bites");
        base.ExposeData();
    }
}