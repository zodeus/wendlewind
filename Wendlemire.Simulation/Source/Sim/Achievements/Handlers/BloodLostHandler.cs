namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player has lost a cumulative amount of blood from damage
/// </summary>
public class BloodLostHandler : AchievementHandler
{
    public BloodLostHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnBloodLost(Pawn pawn, float bloodLost)
    {
        if (pawn.PawnType != PawnType.Player)
        {
            return;
        }

        if (IsUnlocked) return;

        Progress.CurrentValue += bloodLost;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}

