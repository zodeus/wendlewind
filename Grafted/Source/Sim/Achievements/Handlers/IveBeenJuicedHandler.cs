namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player dies from blood loss
/// </summary>
public class IveBeenJuicedHandler : AchievementHandler
{
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || context.PlayerWon) return;

        if (context.CauseOfDeath == "Blood loss")
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        // Start with 2 Jars of Blood
        for (var i = 0; i < 2; i++)
        {
            context.Player.Pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.JarOfBlood));
        }
    }
}


