namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when using a specific item type a certain number of times
/// </summary>
public class ItemUsedHandler : AchievementHandler
{
    public override void OnItemUsed(Pawn consumer, Item item)
    {
        if (IsUnlocked) return;
        
        // Check if this item matches the achievement's target item
        if (Def.ItemDef == null || item.Def != Def.ItemDef) return;
        
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}

