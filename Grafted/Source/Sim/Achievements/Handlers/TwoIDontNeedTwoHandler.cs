namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player loses both eyes and survives
/// </summary>
public class TwoIDontNeedTwoHandler : AchievementHandler
{
    private const float EyeHitPointsMultiplier = 2f;

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        var eyes = context.Player.Body.AllParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count >= 2 && eyes.All(e => e.IsDestroyed))
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var pawn = context.Player.Pawn;
        var eyes = pawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count == 0) return;

        eyes.ForEach(p => p.MaxHitPoints = p.MaxHitPoints * EyeHitPointsMultiplier);
        eyes.ForEach(p => p.HitPoints = p.MaxHitPoints);
    }
}

