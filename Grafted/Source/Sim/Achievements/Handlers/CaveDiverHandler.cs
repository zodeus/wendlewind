namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player explores caves
/// </summary>
public class CaveDiverHandler : AchievementHandler
{
    private static readonly HashSet<BiomeDef> CaveBiomes = [Defs.Biomes.DampCave, Defs.Biomes.Mineshaft];

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        if (CaveBiomes.Contains(context.Zone.BiomeDef))
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }
}

