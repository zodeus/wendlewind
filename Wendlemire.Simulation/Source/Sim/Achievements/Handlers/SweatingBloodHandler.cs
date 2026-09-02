namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player survives a fight with less than 10% blood
/// </summary>
public class SweatingBloodHandler : AchievementHandler
{
    public SweatingBloodHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        if (context.Player.Body.BloodPercent < Def.TargetValue)
        {
            Unlock();
        }
    }

}

