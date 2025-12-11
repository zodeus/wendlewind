namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player finds items (looting from specimens)
/// </summary>
public class TrashToTreasureHandler : AchievementHandler
{
    private List<ItemType> _trashItemTypes = [ItemType.Equipment];

    public override void OnItemDisassembled(Item item)
    {
        if (IsUnlocked) return;

        if (!_trashItemTypes.Contains(item.ItemDef.ItemType)) return;
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var weaponDefs = new List<ItemDef> { Defs.Items.WoodenHammer, Defs.Items.BirchRod, Defs.Items.Knife };

        PawnGenerator.RegisterEquipment(context.Player.Pawn, weaponDefs.InRandomOrder().Take(1).ToList());
    }
}
