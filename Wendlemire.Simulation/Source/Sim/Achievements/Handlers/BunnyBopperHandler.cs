namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of rabbits
/// </summary>
public class BunnyBopperHandler : AchievementHandler
{
    public BunnyBopperHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyKilled(Pawn enemy)
    {
        if (IsUnlocked) return;
        if (enemy.Species != "Rabbit") return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
