namespace Wendlewind.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player kills a certain number of enemies
/// </summary>
public class ButcherHandler : EnemyKilledHandler
{
    public ButcherHandler(IRng rng) : base(rng)
    {
    }

    private const float FingerHitPointsMultiplier = 2f;

    public override void OnWorldRestart(GameContext context)
    {
        if (IsUnlocked == false) return;

        var pawn = context.Player.Pawn;
        var appendages = pawn.Body.AllExternalParts.Where(p => p.Type == BodyPartType.Finger || p.Type == BodyPartType.Thumb).ToList();
        if (appendages.Count == 0) return;
        
        appendages.ForEach(p => p.MaxHitPoints = p.MaxHitPoints * FingerHitPointsMultiplier);
        appendages.ForEach(p => p.HitPoints = p.MaxHitPoints);
        appendages.ForEach(p => p.AllInternalParts.ForEach(ip => ip.AdaptBodyPartTo(p)));
    }
}

