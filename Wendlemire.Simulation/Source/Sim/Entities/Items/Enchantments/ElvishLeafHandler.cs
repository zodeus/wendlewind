namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class ElvishLeafHandler : EnchantmentHandler
{
    public ElvishLeafHandler(IRng rng)
    {
        Rng = rng;
    }

    public const float HealingPerTick = 0.00008f;
    public override void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
        var heal = bodyPart.MaxHitPoints * HealingPerTick * GetMagic(pawn);
        bodyPart.HitPoints += heal;
        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            internalPart.HitPoints += internalPart.MaxHitPoints * HealingPerTick * GetMagic(pawn);
        }
    }
}