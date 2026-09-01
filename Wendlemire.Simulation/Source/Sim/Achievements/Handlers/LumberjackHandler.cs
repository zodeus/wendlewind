namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of Treeborn enemies (cutting down trees).
/// </summary>
public class LumberjackHandler : AchievementHandler
{
    public LumberjackHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyKilled(Pawn enemy)
    {
        if (IsUnlocked) return;
        if (enemy.Species != "Treeborn") return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        // Start with a bone axe
        PawnGenerator.RegisterEquipment(context.Player.Pawn, [Defs.Items.BoneAxe]);
    }
}
