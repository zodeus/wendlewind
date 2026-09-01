namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Context passed when combat ends
/// </summary>
public class AchievementCombatEndContext
{
    public Pawn Player { get; init; } = null!;
    public Pawn Enemy { get; init; } = null!;
    public bool PlayerWon { get; init; }
    public double TotalDamageDealt { get; init; }
    public int CombatTicks { get; init; }
    public Zone Zone { get; init; } = null!;
    public string? CauseOfDeath { get; init; }
}