namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds rocks.
/// </summary>
public class RockHoundHandler : AchievementHandler
{
    public RockHoundHandler(IRng rng)
    {
        Rng = rng;
    }

    private static readonly HashSet<ItemDef> Rocks = [Defs.Items.Rock, Defs.Items.RockOfRot];

    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;
        if (!Rocks.Contains(item.ItemDef)) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
