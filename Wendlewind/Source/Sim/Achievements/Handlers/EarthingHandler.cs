namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player wins combats while barefoot (no boots equipped).
/// "Feel the earth between your toes"
/// </summary>
public class EarthingHandler : AchievementHandler
{
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked || !context.PlayerWon) return;

        // Check if the player is barefoot (no foot armor on any foot)
        var feet = context.Player.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Foot).ToList();
        if (feet.Count == 0) return;

        var isBarefoot = feet.All(foot =>
            !foot.Equipment.ContainsKey(EquipmentSlotType.FootArmor) ||
            foot.Equipment[EquipmentSlotType.FootArmor] == null);

        if (isBarefoot)
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        // Start with a pair of leather boots
        var pawn = context.Player.Pawn;
        var feetCount = pawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Foot).ToList().Count;

        for (var i = 0; i < feetCount; i++)
        {
            PawnGenerator.RegisterEquipment(pawn, [Defs.Items.LeatherBoot]);
        }
    }
}
