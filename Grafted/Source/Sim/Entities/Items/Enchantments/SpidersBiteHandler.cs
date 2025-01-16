namespace Grafted.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class SpidersBiteHandler : EnchantmentHandler
{
    public int Bites;

    public override void HandlePawnTakeDamage(BodyPart bodyPart, Pawn pawn, Pawn target, DamageRecord damageRecord)
    {
        var randomPart = target.Body.AllExternalParts.RandomElement();
        if (randomPart.Skin is not { } skin) return;

        Bites++;
        skin.ApplyBodyPartModifiers(Enchantment.ItemDef.WeaponProperties.BodyPartModifiers, new DamagedBodyPartRecord(skin));
        damageRecord.SourceAfflictions.Add(new AfflictionRecord(randomPart, "Bitten"));
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref Bites!, "Bites");
        base.ExposeData();
    }
}