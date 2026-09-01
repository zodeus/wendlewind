namespace Wendlemire.Sim.Achievements.Handlers;

/// <summary>
/// Unlocks when the player eats a lot of food.
/// Reward: Substantial increase in physical durability (boosts external body part MaxHitPoints).
/// </summary>
[UsedImplicitly]
public class MuscleManTandyRavageHandler : AchievementHandler
{
    public MuscleManTandyRavageHandler(IRng rng)
    {
        Rng = rng;
    }

    private const float ExternalPartHitPointsMultiplier = 1.5f;

    public override void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        if (IsUnlocked || data == null) return;

        var amount = (float)(data?.Amount ?? 0);
        var bodyPart = (BodyPart)data?.BodyPart!;
        if(bodyPart.Type != BodyPartType.Head && bodyPart.Type != BodyPartType.Torso) return;
        Progress.CurrentValue += amount;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    public override void OnWorldRestart(GameContext context)
    {
        if (!IsUnlocked) return;

        var pawn = context.Player.Pawn;
        var externalParts = pawn.Body.AllExternalParts.ToList();
        if (externalParts.Count == 0) return;

        foreach (var part in externalParts)
        {
            part.MaxHitPoints *= ExternalPartHitPointsMultiplier;
            part.HitPoints = part.MaxHitPoints;

            foreach (var internalPart in part.AllInternalParts)
            {
                internalPart.AdaptBodyPartTo(part);
            }
        }
    }
}
