namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills their first enemy
/// </summary>
public class FirstBloodHandler : AchievementHandler
{
    public FirstBloodHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        Unlock();
    }
}

