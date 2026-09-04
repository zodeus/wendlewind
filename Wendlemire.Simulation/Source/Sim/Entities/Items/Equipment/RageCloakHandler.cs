namespace Wendlemire.Sim.Entities.Items.Equipment;

/// <summary>
/// Strength grows with destroyed and severed external parts.
/// </summary>
public class RageCloakHandler : EquipmentHandler, ICloakHandler
{
    public const float RageFactor = 0.08f;

    public RageCloakHandler(IRng rng)
    {
        Rng = rng;
    }

    public static float MultiplierFor(int brokenParts) => 1f + brokenParts * RageFactor;

    public string GetBonusDisplayText() => $"Rage: +{RageFactor:P0} Strength per destroyed or severed part";

    public override void ModifyStat(Pawn pawn, StatDef stat, ref float value)
    {
        if (stat != Defs.Stats.Strength)
        {
            return;
        }

        var broken = SetBonuses.CountBrokenParts(pawn);
        if (broken <= 0)
        {
            return;
        }

        value *= MultiplierFor(broken);
    }
}
