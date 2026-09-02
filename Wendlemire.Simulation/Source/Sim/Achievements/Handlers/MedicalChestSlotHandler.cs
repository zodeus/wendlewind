namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Item-use achievement. Progress comes from the base OnItemUsed + ItemUsedDef path.
/// </summary>
public class MedicalChestSlotHandler : AchievementHandler
{
    public MedicalChestSlotHandler(IRng rng)
    {
        Rng = rng;
    }
}
