using Wendlemire.Sim.Achievements.Handlers;

namespace Wendlemire.Sim.Achievements;

public static class ConsumableSlotUnlocks
{
    public static int UnlockedCapacity(AchievementTracker tracker, Type handlerType, int baseSlots, int maxSlots)
    {
        var extra = SlotUnlockDefs(handlerType).Count(tracker.IsUnlocked);
        return Math.Min(maxSlots, baseSlots + extra);
    }

    public static IEnumerable<AchievementDef> SlotUnlockDefs(Type handlerType)
    {
        return DefRepository<AchievementDef>.Defs
            .Where(d => d.HandlerClass != null && handlerType.IsAssignableFrom(d.HandlerClass));
    }

    public static AchievementDef? NextLocked(AchievementTracker tracker, Type handlerType)
    {
        return SlotUnlockDefs(handlerType).FirstOrDefault(d => !tracker.IsUnlocked(d));
    }
}
