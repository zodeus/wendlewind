namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when severing an enemy's finger or thumb
/// </summary>
public class JustTheTipHandler : AchievementHandler
{
    public JustTheTipHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;
        
        var severedParts = CountDamagedParts(response, static p =>
            (p.BodyPart.Type == BodyPartType.Finger || p.BodyPart.Type == BodyPartType.Thumb) && p.WasSevered);
        if (severedParts <= 0)
        {
            return;
        }

        Progress.CurrentValue += severedParts;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}


