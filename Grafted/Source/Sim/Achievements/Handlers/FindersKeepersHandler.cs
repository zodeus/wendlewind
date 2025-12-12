namespace Grafted.Sim.Achievements.Handlers;

using Grafted.Sim.Entities;
/// <summary>
/// Unlocks when the player finds an item (looting from combat)
/// Benefit: Start with the Grimoire trinket
/// </summary>
public class FindersKeepersHandler : AchievementHandler
{
    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;

        if (item.Def == Defs.Items.Grimoire)
        {
            Unlock();
            return;
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var grimoire = EntityGenerator.CreateEntity<Item>(Defs.Items.Grimoire);
        context.Player.Pawn.Inventory.TryAdd(grimoire);
    }
}

