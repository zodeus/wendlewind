namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds a certain number of trinkets
/// </summary>
public class TrinketSnifferHandler : AchievementHandler
{
    public TrinketSnifferHandler(IRng rng)
    {
        Rng = rng;
    }

    private static List<ItemDef> StarterTrinkets = [Defs.Items.CookingPot, Defs.Items.MortarAndPestle, Defs.Items.TinkersToolbox];
    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;

        if (item.ItemDef.ItemType != ItemType.Trinket) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var trinket = StarterTrinkets.InRandomOrder(Context.Rng).First();
        context.Player.Pawn.Inventory.TryAdd(Context.Factory.CreateEntity<Item>(trinket));
    }
}
