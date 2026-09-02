namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player eats food-poisoning items.
/// </summary>
public class GarbageGremlinHandler : FoodSlotHandler
{
    public GarbageGremlinHandler(IRng rng) : base(rng)
    {
    }


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
