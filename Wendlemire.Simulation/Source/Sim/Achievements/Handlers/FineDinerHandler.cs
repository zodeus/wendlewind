namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player eats well prepared meals
/// </summary>
public class FineDinerHandler : AchievementHandler
{
    public FineDinerHandler(IRng rng)
    {
        Rng = rng;
    }

    private static readonly HashSet<ItemDef> FineFoods = [Defs.Items.HeartyStew, Defs.Items.GoldCapMushroom];

    private static readonly List<ItemDef> AvailableFoods = [
         Defs.Items.HeartyStew, Defs.Items.GoldCapMushroom,
         Defs.Items.DriedMeat, Defs.Items.HoneyPot
    ];

    private static readonly int MaxFoods = 3;
    private static readonly RangeInt FoodStackSize = new RangeInt(1, 5);

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        var foodProps = item.ItemDef.FoodProperties;
        if (foodProps == null) return;

        if (!FineFoods.Contains(item.ItemDef)) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var pawn = context.Player.Pawn;
        for (int i = 0; i < MaxFoods; i++)
        {
            AddFoodToInventory(pawn);
        }
    }

    private void AddFoodToInventory(Pawn pawn)
    {
        var food = AvailableFoods.InRandomOrder(Context.Rng).First();
        var stackSize = FoodStackSize.Roll(Context.Rng);
        pawn.Inventory.TryAdd(Context.Factory.CreateEntity<Item>(food, stackSize));
    }
}

