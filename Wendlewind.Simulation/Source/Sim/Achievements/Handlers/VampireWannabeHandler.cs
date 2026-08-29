namespace Wendlewind.Sim.Achievements.Handlers;

public class VampireWannabeHandler : AchievementHandler
{
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


        var wasBloodBathApplied = response.Damages.Any(d => d.ReflectedEffects.Any(m => m.EffectDef == Defs.Items.BloodBath));

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

