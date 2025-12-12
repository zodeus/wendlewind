namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player explores caves
/// </summary>
public class CaveDiverHandler : AchievementHandler
{
    private static readonly HashSet<ZoneDef> CaveZones = [Defs.Zones.DampCave, Defs.Zones.Mineshaft];

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        if (CaveZones.Contains(context.Zone.ZoneDef))
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }

    
}

