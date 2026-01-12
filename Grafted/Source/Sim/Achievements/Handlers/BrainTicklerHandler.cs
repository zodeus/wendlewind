namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when damaging an enemy's brain
/// </summary>
public class BrainTicklerHandler : AchievementHandler
{
    private const float BrainHitPointsMultiplier = 1.5f;

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

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var pawn = context.Player.Pawn;
        var brain = pawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Brain).FirstOrDefault();
        if (brain == null) return;
        brain.MaxHitPoints = brain.MaxHitPoints * BrainHitPointsMultiplier;
        brain.HitPoints = brain.MaxHitPoints;
    }
}

