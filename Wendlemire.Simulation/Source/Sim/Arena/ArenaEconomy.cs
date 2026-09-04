namespace Wendlemire.Sim.Arena;

/// <summary>
/// Arena gold curve helpers. Lifetime gold is the all-win path before a fight;
/// build budget holds 15% back for shop refresh and the next buy.
/// </summary>
public static class ArenaEconomy
{
    public const float BuildBudgetFactor = 0.85f;

    public static int LifetimeGold(int round)
    {
        var fights = Math.Max(0, round - 1);
        return ArenaRun.StartingGold + fights * ArenaRun.WinGold;
    }

    public static int LifetimeGold(int wins, int losses) =>
        ArenaRun.StartingGold
        + Math.Max(0, wins) * ArenaRun.WinGold
        + Math.Max(0, losses) * ArenaRun.LoseGold;

    public static int BuildBudget(int round) =>
        (int)(LifetimeGold(round) * BuildBudgetFactor);
}
