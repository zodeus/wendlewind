using Wendlemire.Sim.Arena;
using Xunit;

namespace Wendlemire.Tests;

public class ArenaEconomyTests
{
    [Fact]
    public void LifetimeGoldOnAllWinPath()
    {
        Assert.Equal(ArenaRun.StartingGold, ArenaEconomy.LifetimeGold(1));
        Assert.Equal(ArenaRun.StartingGold + ArenaRun.WinGold, ArenaEconomy.LifetimeGold(2));
        Assert.Equal(ArenaRun.StartingGold + 11 * ArenaRun.WinGold, ArenaEconomy.LifetimeGold(12));
    }

    [Fact]
    public void LifetimeGoldCountsWinsAndLosses()
    {
        Assert.Equal(
            ArenaRun.StartingGold + 12 * ArenaRun.WinGold + 3 * ArenaRun.LoseGold,
            ArenaEconomy.LifetimeGold(12, 3));
    }

    [Fact]
    public void BuildBudgetHoldsFifteenPercent()
    {
        var lifetime = ArenaEconomy.LifetimeGold(5);
        Assert.Equal((int)(lifetime * ArenaEconomy.BuildBudgetFactor), ArenaEconomy.BuildBudget(5));
        Assert.True(ArenaEconomy.BuildBudget(5) < lifetime);
    }
}
