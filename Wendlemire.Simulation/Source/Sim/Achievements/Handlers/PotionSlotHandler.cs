namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Item-use achievement that unlocks one potion slot.
/// Progress comes from the base OnItemUsed + ItemUsedDef path.
/// </summary>
public class PotionSlotHandler : AchievementHandler
{
    public PotionSlotHandler(IRng rng)
    {
        Rng = rng;
    }
}
