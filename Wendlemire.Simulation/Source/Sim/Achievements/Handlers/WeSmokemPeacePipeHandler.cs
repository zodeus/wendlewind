namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player burns wood consumables
/// </summary>
public class WeSmokemPeacePipeHandler : AchievementHandler
{
    public WeSmokemPeacePipeHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        if (item.ItemDef.ItemType != ItemType.Incense) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
