namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player dies from blood loss
/// </summary>
public class IveBeenJuicedHandler : AchievementHandler
{
    public IveBeenJuicedHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || context.PlayerWon) return;

        if (context.CauseOfDeath == "Blood loss")
        {
            Unlock();
        }
    }

}

