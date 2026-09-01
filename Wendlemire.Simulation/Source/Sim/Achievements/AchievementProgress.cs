namespace Wendlemire.Sim.Achievements;

/// <summary>
/// Tracks progress towards a single achievement
/// </summary>
public class AchievementProgress : IExposable
{
    public AchievementDef Def = null!;
    public float CurrentValue;
    public bool IsUnlocked;
    public DateTime? UnlockedAt;
    
    /// <summary>
    /// Whether the player has dismissed/acknowledged this achievement notification.
    /// </summary>
    public bool IsAcknowledged;

    public AchievementProgress() { }

    public AchievementProgress(AchievementDef def)
    {
        Def = def;
        CurrentValue = 0;
        IsUnlocked = false;
        IsAcknowledged = false;
    }

    public float ProgressPercent => Def.TargetValue > 0 ? Math.Min(1f, (float)CurrentValue / Def.TargetValue) : 0f;

    public void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref CurrentValue, "CurrentValue");
        ScribeValues.Look(ref IsUnlocked, "IsUnlocked");
        ScribeValues.Look(ref UnlockedAt, "UnlockedAt");
        ScribeValues.Look(ref IsAcknowledged, "IsAcknowledged");
    }
}


