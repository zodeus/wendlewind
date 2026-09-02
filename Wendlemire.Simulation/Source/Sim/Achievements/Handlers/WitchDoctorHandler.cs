namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player uses medicinal items.
/// "Use medicinal items"
/// </summary>
public class WitchDoctorHandler : AchievementHandler
{
    public WitchDoctorHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        // Check if the item is medicinal (Medical type)
        var isMedicinal = item.ItemDef.ItemType == ItemType.Medical;
        if (!isMedicinal) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
