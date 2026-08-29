namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player feeds the golden goose to full hunger (100).
/// Benefit: Start with some golden beans.
/// </summary>
public class FullMaxTheGooseHandler : AchievementHandler
{
    private static RangeInt GoldenBeansToGive = new(2, 4);

    public override void OnGooseFed(int currentHunger, int maxHunger)
    {
        if (IsUnlocked) return;

        if (currentHunger >= maxHunger)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var pawn = context.Player.Pawn;
        var goldenBeans = EntityGenerator.CreateEntity<Item>(Defs.Items.GoldenBean, GoldenBeansToGive.RandomValue);
        pawn.Inventory.TryAdd(goldenBeans);
    }
}
