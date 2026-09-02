using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Cosmetics;

namespace Wendlemire.NetCode;

public static class ArenaMarks
{
    public const int PerWin = 10;
    public const int VictoryBonus = 50;
    public const int GoldPerMark = 10;
    public const string DefaultNamePlate = CosmeticDefaults.NamePlate;

    public static int ForFinishedRun(int wins, int finalGold)
    {
        var victory = wins >= ArenaRun.WinsToFinish;
        return (Math.Max(0, wins) * PerWin)
               + (victory ? VictoryBonus : 0)
               + (Math.Max(0, finalGold) / GoldPerMark);
    }
}
