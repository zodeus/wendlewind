namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player loses an arm or leg
/// </summary>
public class TisButAScratchHandler : AchievementHandler
{
    public TisButAScratchHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnPlayerDamaged(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;

        var severedParts = CountDamagedParts(response, static p =>
            (p.BodyPart.Type == BodyPartType.Arm || p.BodyPart.Type == BodyPartType.Leg) && p.WasSevered);
        if (severedParts >= Def.TargetValue)
        {
            Unlock();
        }
    }

}
