namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player loses an arm or leg
/// </summary>
public class TisButAScratchHandler : AchievementHandler
{
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

        var armorDefs = DefRepository<ItemDef>.Defs.Where(d => d.EquipmentProperties?.EquipmentType == EquipmentType.Armor).ToList();

        PawnGenerator.RegisterEquipment(context.Player.Pawn, armorDefs.InRandomOrder().Take(1).ToList());
    }
}


