namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player consumes a lot of food (tracks total nutrition consumed).
/// Reward: PotBellied trait - more stomach capacity.
/// </summary>
public class YouLittlePiggyHandler : AchievementHandler
{
    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        var foodProps = item.ItemDef.FoodProperties;
        if (foodProps == null) return;

        var nutrition = item.GetStatValue(Defs.Stats.NutritionalValue);
        Progress.CurrentValue += nutrition;

        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}


