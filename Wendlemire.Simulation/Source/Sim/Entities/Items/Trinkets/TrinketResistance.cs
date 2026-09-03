namespace Wendlemire.Sim.Entities.Items.Trinkets;

public static class TrinketResistance
{
    public static float SumFor(Pawn pawn, DamageType type)
    {
        var stat = type.GetResistanceStat();
        if (stat == null)
        {
            return 0f;
        }

        var total = 0f;
        foreach (var trinket in pawn.Inventory.Trinkets)
        {
            total += trinket.GetStatValue(stat);
        }

        return total;
    }

    public static Item? FirstContributor(Pawn pawn, DamageType type)
    {
        var stat = type.GetResistanceStat();
        if (stat == null)
        {
            return null;
        }

        foreach (var trinket in pawn.Inventory.Trinkets)
        {
            if (trinket.GetStatValue(stat) > 0f)
            {
                return trinket;
            }
        }

        return null;
    }
}
