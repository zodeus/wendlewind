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

}
