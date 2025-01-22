namespace Grafted.Sim.Entities.Items.Enchantments;

public abstract class EnchantmentHandler : IExposable
{
    public Item Enchantment = null!;

    public string Label => Enchantment.Label;

    public virtual void Tick()
    {
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Enchantment!, "Def");
    }

    public override string ToString()
    {
        return $"{Enchantment.Label}";
    }

    public abstract void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord);
}