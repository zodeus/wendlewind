namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds items (looting from specimens)
/// </summary>
public class TrashToTreasureHandler : AchievementHandler
{
    public TrashToTreasureHandler(IRng rng)
    {
        Rng = rng;
    }

    private List<ItemType> _trashItemTypes = [ItemType.Equipment];

    public override void OnItemDisassembled(Item item)
    {
        if (IsUnlocked) return;

        if (!_trashItemTypes.Contains(item.ItemDef.ItemType)) return;
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
