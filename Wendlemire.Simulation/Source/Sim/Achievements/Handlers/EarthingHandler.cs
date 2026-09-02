namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player wins combats while barefoot (no boots equipped).
/// "Feel the earth between your toes"
/// </summary>
public class EarthingHandler : AchievementHandler
{
    public EarthingHandler(IRng rng)
    {
        Rng = rng;
    }

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

}
