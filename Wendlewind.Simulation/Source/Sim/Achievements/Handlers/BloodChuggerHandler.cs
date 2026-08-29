namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player consumes jars of blood.
/// Benefit: Increases max blood capacity.
/// </summary>
public class BloodChuggerHandler : AchievementHandler
{
    public BloodChuggerHandler(IRng rng)
    {
        Rng = rng;
    }

    private const float MaxBloodBonus = 500f;

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        context.Player.Pawn.Body.MaxBloodBonus += MaxBloodBonus;
    }
}


