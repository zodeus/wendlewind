namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when severing an enemy's finger or thumb
/// </summary>
public class JustTheTipHandler : AchievementHandler
{
    public JustTheTipHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;
        
        var severedParts = response.Damages
        .SelectMany(d => d.BodyParts)
        .Concat(response.TrinketDamages.SelectMany(d => d.BodyParts))
        .Where(p => (p.BodyPart.Type == BodyPartType.Finger || p.BodyPart.Type == BodyPartType.Thumb) && p.WasSevered)
        .ToList();
        foreach (var damage in severedParts)
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }
}


