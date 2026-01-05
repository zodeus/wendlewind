namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player uses medicinal items.
/// "Use medicinal items"
/// </summary>
public class WitchDoctorHandler : AchievementHandler
{
    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked) return;

        // Check if the item is medicinal (Medical type)
        var isMedicinal = item.ItemDef.ItemType == ItemType.Medical;
        if (!isMedicinal) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        Log.Info("Witch Doctor achievement unlocked, giving rejuvenation cloak");
        // Start with a cloak of rejuvenation
        var pawn = context.Player.Pawn;
        PawnGenerator.RegisterEquipment(pawn, [Defs.Items.RejuvenationCloak]);
    }
}
