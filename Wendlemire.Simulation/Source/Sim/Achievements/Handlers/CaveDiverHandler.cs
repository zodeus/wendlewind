namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Progresses by winning fights with incense lit.
/// </summary>
public class CaveDiverHandler : IncenseSlotHandler
{
    public CaveDiverHandler(IRng rng) : base(rng)
    {
    }

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
    }

    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon)
        {
            return;
        }

        if (context.Player.ActiveIncense.Count == 0)
        {
            return;
        }

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}
