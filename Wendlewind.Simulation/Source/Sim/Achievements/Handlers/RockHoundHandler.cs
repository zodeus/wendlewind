namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds rocks.
/// </summary>
public class RockHoundHandler : AchievementHandler
{
    public RockHoundHandler(IRng rng)
    {
        Rng = rng;
    }

    private static readonly HashSet<ItemDef> Rocks = [Defs.Items.Rock, Defs.Items.RockOfRot];

    private static readonly RangeInt RotRockStackSize = new(2, 4);
    private static readonly RangeInt NormalRockStackSize = new(3, 5);

    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;
        if (!Rocks.Contains(item.ItemDef)) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var pawn = context.Player.Pawn;

        // Add rot rocks
        var rotRockCount = RotRockStackSize.Roll(Context.Rng);
        pawn.Inventory.TryAdd(Context.Factory.CreateEntity<Item>(Defs.Items.RockOfRot, rotRockCount));

        // Add normal rocks
        var normalRockCount = NormalRockStackSize.Roll(Context.Rng);
        pawn.Inventory.TryAdd(Context.Factory.CreateEntity<Item>(Defs.Items.Rock, normalRockCount));
    }
}
