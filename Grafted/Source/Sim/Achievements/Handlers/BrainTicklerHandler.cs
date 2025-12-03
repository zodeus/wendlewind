namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when damaging an enemy's brain
/// </summary>
public class BrainTicklerHandler : AchievementHandler
{
    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked || response == null) return;

        var allDamages = response.Damages
            .SelectMany(d => d.BodyParts)
            .Concat(response.TrinketDamages.SelectMany(d => d.BodyParts))
            .ToList();
        if (allDamages.Any(p => p.BodyPart.Type == BodyPartType.Brain))
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }
}

