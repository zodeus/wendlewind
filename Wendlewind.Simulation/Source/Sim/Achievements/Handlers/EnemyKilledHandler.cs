namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of enemies
/// </summary>
public class EnemyKilledHandler : AchievementHandler
{
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}

