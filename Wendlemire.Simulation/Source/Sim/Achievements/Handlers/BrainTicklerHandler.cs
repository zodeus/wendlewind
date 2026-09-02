namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when damaging an enemy's brain
/// </summary>
public class BrainTicklerHandler : AchievementHandler
{
    public BrainTicklerHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked || response == null) return;

        if (AnyDamagedPart(response, static p => p.BodyPart.Type == BodyPartType.Brain))
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }

}

