namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of enemies
/// </summary>
public class TheExecutionersBitchHandler : AchievementHandler
{
    public TheExecutionersBitchHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnPlayerDamaged(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked == true) return;

        if (!AnyDamagedPart(response, static p => p.BodyPart.Type == BodyPartType.Neck)) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}

