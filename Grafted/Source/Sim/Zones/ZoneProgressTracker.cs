namespace Grafted.Sim.Zones;

/// <summary>
/// Tracks the furthest zone stage the player has ever reached.
/// This progress persists across StartOver/reset - it never decreases.
/// </summary>
public class ZoneProgressTracker : IExposable
{
    /// <summary>
    /// The highest stage number ever completed by the player.
    /// Stage numbers match ZoneDef.Stage values.
    /// </summary>
    public int HighestStageCompleted;

    /// <summary>
    /// Updates the tracker when a zone is completed.
    /// Only increases if the completed stage is higher than previous best.
    /// </summary>
    public void OnZoneCompleted(Zone zone)
    {
        var stage = zone.ZoneDef.Stage;
        if (stage > HighestStageCompleted)
        {
            HighestStageCompleted = stage;
        }
    }

    /// <summary>
    /// Checks if the player has ever reached this zone before.
    /// </summary>
    public bool HasEverReached(Zone zone)
    {
        return zone.ZoneDef.Stage <= HighestStageCompleted + 1;
    }

    /// <summary>
    /// Checks if this zone represents the player's personal best boundary.
    /// Returns true if this zone was the next challenge after previous best
    /// (i.e., you've beaten all zones before this one in a previous run).
    /// Only meaningful when player has made previous progress.
    /// </summary>
    public bool IsFurthestEverReached(Zone zone)
    {
        // Only show if player has made progress before
        if (HighestStageCompleted == 0) return false;
        
        // Show on the zone right after the highest completed (the "wall" you hit)
        return zone.ZoneDef.Stage == HighestStageCompleted + 1;
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref HighestStageCompleted, "HighestStageCompleted");
    }
}
