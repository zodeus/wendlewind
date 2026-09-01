namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Historical achievement kept so existing saves still load.
/// The Grimoire is now a first-class system and is no longer a findable trinket.
/// </summary>
public class FindersKeepersHandler : AchievementHandler
{
    public FindersKeepersHandler(IRng rng)
    {
        Rng = rng;
    }
}
