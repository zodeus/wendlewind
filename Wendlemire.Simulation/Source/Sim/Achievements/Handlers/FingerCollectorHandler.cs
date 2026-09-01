namespace Wendlemire.Sim.Achievements.Handlers;

public class FingerCollectorHandler : AchievementHandler
{
    public FingerCollectorHandler(IRng rng)
    {
        Rng = rng;
    }

    private const float FingerHitPointsMultiplier = 1.5f;

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

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var pawn = context.Player.Pawn;
        var fingers = pawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Finger).ToList();
        if (fingers.Count == 0) return;
        
        fingers.ForEach(p => p.MaxHitPoints = p.MaxHitPoints * FingerHitPointsMultiplier);
        fingers.ForEach(p => p.HitPoints = p.MaxHitPoints);
        fingers.ForEach(p => p.AllInternalParts.ForEach(ip => ip.AdaptBodyPartTo(p)));
    }
}

