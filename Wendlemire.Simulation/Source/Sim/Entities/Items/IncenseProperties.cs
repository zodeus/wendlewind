namespace Wendlemire.Sim.Entities.Items;

public class IncenseProperties
{
    public const int BaseSlots = 1;
    public const int MaxActive = 3;
    public const int SlotIgniteIntervalTicks = 120;

    public BodyEffectRecord Effect = null!;
    public int DurationInEncounters;

    public static int GetIgniteTick(int slotIndex)
    {
        return (slotIndex + 1) * SlotIgniteIntervalTicks;
    }

    public int GetDurationInEncounters()
    {
        return DurationInEncounters > 0 ? DurationInEncounters : 1;
    }

    public int GetDurationInTicks()
    {
        var ticks = Effect?.DurationInTicks ?? 0;
        return ticks > 0 ? ticks : GameContext.TicksPerSecond * 20;
    }

    public static Color GetEffectColor(BodyEffectDef def)
    {
        if (def.AffectedStats != null)
        {
            foreach (var stat in def.AffectedStats)
            {
                if (stat.Offset < 0 || stat.Factor < 0f)
                    return new Color(220, 100, 100); // Negative effect - red
            }
        }
        return new Color(255, 200, 120); // Positive/neutral effect - warm glow
    }
}
