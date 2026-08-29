namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player loses an arm or leg
/// </summary>
public class TisButAScratchHandler : AchievementHandler
{
    public TisButAScratchHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnPlayerDamaged(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;

        var severedParts = response.Damages
        .SelectMany(d => d.BodyParts)
        .Concat(response.TrinketDamages.SelectMany(d => d.BodyParts))
        .Where(p => (p.BodyPart.Type == BodyPartType.Arm || p.BodyPart.Type == BodyPartType.Leg) && p.WasSevered)
        .ToList();
        if (severedParts.Count >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var armorDefs = new List<ItemDef> {
            Defs.Items.FishBowlHelmet, Defs.Items.LeatherGlove, Defs.Items.LeatherBoot, Defs.Items.LeatherVambrace, Defs.Items.BucketHelmet,
            Defs.Items.ClothHelmet, Defs.Items.ClothTunic, Defs.Items.ClothGorget
        };

        PawnGenerator.RegisterEquipment(context.Player.Pawn, armorDefs.InRandomOrder(Context.Rng).Take(1).ToList());
    }
}
