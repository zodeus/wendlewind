namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player survives a fight with less than 10% blood
/// </summary>
public class SweatingBloodHandler : AchievementHandler
{
    private const float MaxBloodBonus = 500f;
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        if (context.Player.Body.BloodPercent < Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        context.Player.Pawn.Body.MaxBloodBonus += MaxBloodBonus;
    }
}

