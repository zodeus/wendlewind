namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player dies with items in their inventory multiple times.
/// "You know that loser who hangs on to shit and dies with a full inventory? You're that loser."
/// </summary>
public class SquirelyPeteHandler : AchievementHandler
{
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || context.PlayerWon == false) return;

        var maxStackFound = context.Player.Inventory.Max(i => i.StackSize);
        if (maxStackFound > Progress.CurrentValue) {
            Progress.CurrentValue = maxStackFound;
        }

        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }   
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        context.Player.Pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.Walnut, Core.Random.Next(1, 2)));
    }
}
