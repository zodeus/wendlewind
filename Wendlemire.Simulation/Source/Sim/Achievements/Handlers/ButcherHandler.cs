namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of enemies
/// </summary>
public class ButcherHandler : EnemyKilledHandler
{
    public ButcherHandler(IRng rng) : base(rng)
    {
    }

}

