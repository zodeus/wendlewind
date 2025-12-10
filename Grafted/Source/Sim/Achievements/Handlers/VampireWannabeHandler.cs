namespace Grafted.Sim.Achievements.Handlers;

public class VampireWannabeHandler : AchievementHandler
{
    public override void OnPlayerDamaged(Pawn pawn, DamageRequest request, DamageResponse response)
    {
        if (IsUnlocked) return;


        var wasBloodBathApplied = response.Damages.Any(d => d.DamageStatusEffects.Any(m => m.EffectDef == Defs.Items.BloodBath));
        
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

