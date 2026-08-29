namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds items (looting from specimens)
/// </summary>
public class GarbageGremlinHandler : AchievementHandler
{

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        var hasFoodPoisoning = item.ItemDef.FoodProperties?.Effects.Any(e => e.Def == Defs.BodyEffects.FoodPoisoning) == true;
        if (hasFoodPoisoning == false) return;
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
