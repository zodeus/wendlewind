namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player burns wood consumables
/// </summary>
public class WeSmokemPeacePipeHandler : AchievementHandler
{
    public override void OnItemUsed(Pawn consumer, Item item)
    {
        if (IsUnlocked) return;

        if (item.ItemDef.ItemType != ItemType.Flammable) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
