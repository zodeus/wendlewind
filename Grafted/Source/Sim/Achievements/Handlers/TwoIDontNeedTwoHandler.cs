namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player loses both eyes and survives
/// </summary>
public class TwoIDontNeedTwoHandler : AchievementHandler
{
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;
        
        var eyes = context.Player.Body.AllParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count >= 2 && eyes.All(e => e.IsDestroyed))
        {
            Unlock();
        }
    }
}

