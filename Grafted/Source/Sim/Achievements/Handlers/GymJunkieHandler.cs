namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player consumes food that gives the BeefedUp effect.
/// "Have you been juicing at the gym?"
/// </summary>
[UsedImplicitly]
public class GymJunkieHandler : AchievementHandler
{
    public override void OnItemUsed(Pawn consumer, Item item)
    {
        if (IsUnlocked) return;

        if (item.ItemDef != Defs.Items.SteroidInjector) return;
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        PawnGenerator.RegisterEquipment(context.Player.Pawn, [Defs.Items.StrengthCloak]);
    }
}
