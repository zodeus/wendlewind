namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player uses repair kits.
/// Reward: ApprenticeFixer trait - repairing items also increases max durability by 10%.
/// </summary>
public class WorkWorkHandler : AchievementHandler
{
    public WorkWorkHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        if (item.Def != Defs.Items.RepairKit) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
