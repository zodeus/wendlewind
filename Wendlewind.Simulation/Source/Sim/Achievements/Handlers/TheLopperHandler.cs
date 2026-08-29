namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when severing enemy heads
/// </summary>
public class TheLopperHandler : AchievementHandler
{
    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;

        var severedHeads = response.Damages
            .SelectMany(d => d.BodyParts)
            .Concat(response.TrinketDamages.SelectMany(d => d.BodyParts))
            .Where(p => p.BodyPart.Type == BodyPartType.Head && p.WasSevered)
            .ToList();

        foreach (var _ in severedHeads)
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }
}

