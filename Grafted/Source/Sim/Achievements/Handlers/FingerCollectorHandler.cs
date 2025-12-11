namespace Grafted.Sim.Achievements.Handlers;

public class FingerCollectorHandler : AchievementHandler
{
    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;

        var severedFingerCount = response.Damages
            .SelectMany(d => d.BodyParts)
            .Concat(response.TrinketDamages.SelectMany(d => d.BodyParts))
            .Where(p => p.BodyPart.Type == BodyPartType.Finger && p.WasSevered)
            .Count();

        Progress.CurrentValue += severedFingerCount;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}

