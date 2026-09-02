namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds a certain number of trinkets
/// </summary>
public class TrinketSnifferHandler : AchievementHandler
{
    public TrinketSnifferHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;

        if (item.ItemDef.ItemType != ItemType.Trinket) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
