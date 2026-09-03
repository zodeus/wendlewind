namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class EnchantmentHandler : IExposable, IHasContext, IHasRng
{
    public GameContext Context { get; set; } = null!;
    public IRng Rng { get; set; } = null!;
    public Item Enchantment = null!;

    public string Label => Enchantment.Label;

    public virtual void Tick()
    {
    }
    public virtual void TickForPawn(Pawn pawn, BodyPart bodyPart)
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

    public virtual void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn target, Pawn source, DamageRecord damageRecord)
    {
    }

    protected float GetMagic(Pawn pawn) => pawn.GetStatValue(Defs.Stats.Magic);

    protected Item? HostEquipment(Pawn pawn) => ItemSynergies.HostOf(pawn, Enchantment);

    protected bool HostHasEnchant(Pawn pawn, ItemDef def) =>
        ItemSynergies.HostHas(HostEquipment(pawn), def);

    protected string? HostItemMoniker(BodyPart bodyPart)
    {
        return bodyPart.Equipment.Values
            .FirstOrDefault(item => item?.Enchantments?.Contains(Enchantment) == true)
            ?.ItemDef.Moniker;
    }
}