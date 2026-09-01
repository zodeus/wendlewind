namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player holds 50 golden beans.
/// Benefit: Unlocks the Golden Goose trinket.
/// </summary>
public class JackAndTheBeanstalkHandler : AchievementHandler
{
    public JackAndTheBeanstalkHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;

        var pawn = Context.Player.Pawn;
        var goldenBeans = pawn.Inventory.AmountOf(Defs.Items.GoldenBean);

        // only update if progress is greater than the current value
        if (goldenBeans > Progress.CurrentValue)
        {
            Progress.CurrentValue = goldenBeans;
        }

        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
