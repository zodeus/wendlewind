namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player eats well prepared meals
/// </summary>
public class FineDinerHandler : AchievementHandler
{
    private static readonly HashSet<ItemDef> FineFoods = [Defs.Items.HeartyStew, Defs.Items.GoldCapMushroom];

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        var foodProps = item.ItemDef.FoodProperties;
        if (foodProps == null) return;

        if (!FineFoods.Contains(item.ItemDef)) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}

