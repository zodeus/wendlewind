namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player feeds the golden goose to full hunger (100).
/// Benefit: Start with some golden beans.
/// </summary>
public class FullMaxTheGooseHandler : AchievementHandler
{
    public FullMaxTheGooseHandler(IRng rng)
    {
        Rng = rng;
    }

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
        var goldenBeans = Context.Factory.CreateEntity<Item>(Defs.Items.GoldenBean, GoldenBeansToGive.Roll(Context.Rng));
        pawn.Inventory.TryAdd(goldenBeans);
    }
}
