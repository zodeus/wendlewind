namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Marker handler for achievements that unlock a food slot.
/// Subclasses supply the progress condition.
/// </summary>
public class FoodSlotHandler : AchievementHandler
{
    public FoodSlotHandler(IRng rng)
    {
        Rng = rng;
    }
}
