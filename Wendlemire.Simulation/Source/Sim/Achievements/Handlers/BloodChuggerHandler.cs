namespace Wendlemire.Sim.Achievements.Handlers;

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

}

