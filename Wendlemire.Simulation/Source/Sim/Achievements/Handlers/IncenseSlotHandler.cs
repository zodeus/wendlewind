namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks one incense slot. Counts any incense use when ItemUsedDef is unset.
/// </summary>
public class IncenseSlotHandler : AchievementHandler
{
    public IncenseSlotHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked)
        {
            return;
        }

        if (Def.ItemUsedDef != null)
        {
            if (item.Def != Def.ItemUsedDef)
            {
                return;
            }

            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }

            return;
        }

        if (item.ItemDef.ItemType != ItemType.Incense)
        {
            return;
        }

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
