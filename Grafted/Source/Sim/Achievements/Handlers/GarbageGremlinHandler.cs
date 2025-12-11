namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds items (looting from specimens)
/// </summary>
public class GarbageGremlinHandler : AchievementHandler
{

    public override void OnItemUsed(Pawn consumer, Item item)
    {
        if (IsUnlocked) return;
        if (item.ItemDef.FoodProperties?.Effects.Any(e => e.Def == Defs.BodyEffects.FoodPoisoning) == false) return;
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
