namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of enemies
/// </summary>
public class TheExecutionersBitchHandler : AchievementHandler
{
    private const float NeckHitPointsMultiplier = 2f;

    public override void OnPlayerDamaged(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked == false) return;

        var hasNeckDamage = response.Damages
        .SelectMany(d => d.BodyParts)
        .Any(p => p.BodyPart.Type == BodyPartType.Neck);

        if (hasNeckDamage == false) return;

        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var pawn = context.Player.Pawn;
        var neck = pawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Neck).FirstOrDefault();
        if (neck == null) return;

        neck.MaxHitPoints = neck.MaxHitPoints * NeckHitPointsMultiplier;
        neck.HitPoints = neck.MaxHitPoints;
        neck.AllInternalParts.ForEach(p => p.AdaptBodyPartTo(neck));
    }
}

