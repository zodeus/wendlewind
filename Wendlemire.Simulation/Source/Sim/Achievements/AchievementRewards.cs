namespace Wendlemire.Sim.Achievements;

public static class AchievementRewards
{
    public const int MarksPerUnlock = 15;

    public static int MarksFor(string? moniker)
    {
        if (string.IsNullOrWhiteSpace(moniker))
        {
            return 0;
        }

        var def = DefRepository<AchievementDef>.GetByMoniker(moniker, raiseError: false);
        return def != null && def.MarksReward > 0 ? def.MarksReward : MarksPerUnlock;
    }
}
