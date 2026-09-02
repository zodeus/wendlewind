namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Item-use achievement. Progress comes from the base OnItemUsed + ItemUsedDef path.
/// </summary>
public class PotionSlotHandler : AchievementHandler
{
    public PotionSlotHandler(IRng rng)
    {
        Rng = rng;
    }
}
