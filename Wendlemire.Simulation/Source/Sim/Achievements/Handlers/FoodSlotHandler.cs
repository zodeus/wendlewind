namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Marker handler for food-consumption achievements. Subclasses supply the progress condition.
/// </summary>
public class FoodSlotHandler : AchievementHandler
{
    public FoodSlotHandler(IRng rng)
    {
        Rng = rng;
    }
}
