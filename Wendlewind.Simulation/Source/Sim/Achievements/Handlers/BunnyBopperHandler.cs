namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of rabbits
/// </summary>
public class BunnyBopperHandler : AchievementHandler
{
    public override void OnEnemyKilled(Pawn enemy)
    {
        if (IsUnlocked) return;
        if (enemy.Species != "Rabbit") return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var armorDefs = new List<ItemDef> {
            Defs.Items.LeatherGlove, Defs.Items.LeatherBoot, Defs.Items.LeatherVambrace, Defs.Items.BucketHelmet,
            Defs.Items.ClothHelmet, Defs.Items.ClothTunic, Defs.Items.ClothGorget
        };

        PawnGenerator.RegisterEquipment(context.Player.Pawn, armorDefs.InRandomOrder().Take(1).ToList());
    }
}
