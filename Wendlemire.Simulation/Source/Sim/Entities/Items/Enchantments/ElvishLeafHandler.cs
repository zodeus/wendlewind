namespace Wendlemire.Sim.Entities.Items.Enchantments;

[UsedImplicitly]
public class ElvishLeafHandler : EnchantmentHandler
{
    public ElvishLeafHandler(IRng rng)
    {
        Rng = rng;
    }

    public const float HealingPerTick = 0.00008f;

    private static readonly float[] CopyWeights =
    [
        1.00f,
        0.90f,
        0.80f,
        0.70f,
        0.25f,
        0.15f
    ];

    private const float ExtraCopyWeight = 0.10f;

    public override void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
        var magic = GetMagic(pawn) * StackEffectiveness(pawn);
        var heal = bodyPart.MaxHitPoints * HealingPerTick * magic;
        bodyPart.HitPoints += heal;
        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            internalPart.HitPoints += internalPart.MaxHitPoints * HealingPerTick * magic;
        }
    }

    private float StackEffectiveness(Pawn pawn)
    {
        var count = 0;
        foreach (var item in pawn.Equipment)
        {
            if (item.Enchantments == null)
            {
                continue;
            }

            foreach (var socketed in item.Enchantments)
            {
                if (socketed.ItemDef == Enchantment.ItemDef)
                {
                    count++;
                }
            }
        }

        if (count <= 1)
        {
            return 1f;
        }

        var total = 0f;
        for (var i = 0; i < count; i++)
        {
            total += i < CopyWeights.Length ? CopyWeights[i] : ExtraCopyWeight;
        }

        return total / count;
    }
}
