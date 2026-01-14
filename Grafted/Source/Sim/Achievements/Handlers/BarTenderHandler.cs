namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player crafts 20 potions.
/// Benefit: Start with 3 random potions.
/// </summary>
public class BarTenderHandler : AchievementHandler
{
    private const int PotionsToGive = 3;

    public override void OnItemCrafted(Pawn crafter, ItemDef itemDef, int amount)
    {
        if (IsUnlocked) return;
        if (itemDef.ItemType != ItemType.Potion) return;

        Progress.CurrentValue += amount;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var pawn = context.Player.Pawn;
        var availablePotions = DefRepository<ItemDef>.Defs
            .Where(d => d.ItemType == ItemType.Potion)
            .ToList();

        if (availablePotions.Count == 0) return;

        for (int i = 0; i < PotionsToGive; i++)
        {
            var randomPotion = availablePotions.RandomElement();
            pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(randomPotion, 1));
        }
    }
}
