namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player crafts 20 potions.
/// Benefit: Start with 3 random potions.
/// </summary>
public class BarTenderHandler : AchievementHandler
{
    public BarTenderHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemCrafted(Pawn crafter, ItemDef itemDef, int amount)
    {
        if (IsUnlocked) return;
        if (itemDef.ItemType != ItemType.Potion) return;

        Progress.CurrentValue += amount;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
