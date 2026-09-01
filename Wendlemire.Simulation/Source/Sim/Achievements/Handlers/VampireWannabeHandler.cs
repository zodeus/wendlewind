namespace Wendlemire.Sim.Achievements.Handlers;

public class VampireWannabeHandler : AchievementHandler
{
    public VampireWannabeHandler(IRng rng)
    {
        Rng = rng;
    }

    //On combat end, if enemy blood type matchs the player blood type increase blood amount by 5%
    public override void OnCombatEnd(AchievementCombatEndContext context)
    {
        if (IsUnlocked) return;

        if (context.Enemy.Body.Def.BloodType == context.Player.Body.Def.BloodType)
        {
            context.Player.Body.BloodAmount += context.Player.Body.MaxBlood * 0.05f;
        }
    }

    public override void OnPlayerDamaged(Pawn pawn, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;


        var wasBloodBathApplied = false;
        foreach (var damage in response.Damages)
        {
            foreach (var effect in damage.ReflectedEffects)
            {
                if (effect.EffectDef == Defs.Items.BloodBath)
                {
                    wasBloodBathApplied = true;
                    break;
                }
            }

            if (wasBloodBathApplied)
            {
                break;
            }
        }

        if (wasBloodBathApplied)
        {
            Progress.CurrentValue++;
            if (Progress.CurrentValue >= Def.TargetValue)
            {
                Unlock();
            }
        }
    }
}

