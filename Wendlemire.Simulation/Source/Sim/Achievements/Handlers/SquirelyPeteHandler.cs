namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player dies with items in their inventory multiple times.
/// "You know that loser who hangs on to shit and dies with a full inventory? You're that loser."
/// </summary>
public class SquirelyPeteHandler : AchievementHandler
{
    public SquirelyPeteHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || context.PlayerWon == false) return;

        var maxStackFound = context.Player.Inventory
            .Select(item => item.StackSize)
            .DefaultIfEmpty(0)
            .Max();
        if (maxStackFound > Progress.CurrentValue) {
            Progress.CurrentValue = maxStackFound;
        }

        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }   
    }

}
