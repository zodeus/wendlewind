namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player feeds the golden goose to full hunger (100).
/// Benefit: Start with some golden beans.
/// </summary>
public class FullMaxTheGooseHandler : AchievementHandler
{
    public FullMaxTheGooseHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnGooseFed(int currentHunger, int maxHunger)
    {
        if (IsUnlocked) return;

        if (currentHunger >= maxHunger)
        {
            Unlock();
        }
    }

}
