namespace Wendlewind.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class ElvishLeafHandler : EnchantmentHandler
{
    public const float HealingPerTick = 0.0018f;
    public override void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
        bodyPart.HitPoints += bodyPart.MaxHitPoints * HealingPerTick;
        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            internalPart.HitPoints += internalPart.MaxHitPoints * HealingPerTick;
        }
    }
}