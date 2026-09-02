namespace Wendlemire.Sim.Achievements.Handlers;

public class FingerCollectorHandler : AchievementHandler
{
    public FingerCollectorHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;

        var severedFingerCount = CountDamagedParts(response, static p => p.BodyPart.Type == BodyPartType.Finger && p.WasSevered);

        Progress.CurrentValue += severedFingerCount;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

}

