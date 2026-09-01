namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when severing enemy heads
/// </summary>
public class TheLopperHandler : AchievementHandler
{
    public TheLopperHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;

        var severedHeads = CountDamagedParts(response, static p => p.BodyPart.Type == BodyPartType.Head && p.WasSevered);
        if (severedHeads <= 0)
        {
            return;
        }

        Progress.CurrentValue += severedHeads;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }
}

