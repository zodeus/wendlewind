namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Strength Potion - grants a temporary +2 Strength bonus.
/// </summary>
[UsedImplicitly]
public class StrengthPotionHandler : PotionHandler
{
    public StrengthPotionHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;

    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        var actualTarget = user;
        var duration = GetDuration();

        // Apply the Strengthened body effect (+2 Strength)
        actualTarget.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Strengthened,
            TicksLeft = duration
        });

        var message = $"/c[{TC.Attacker}]{actualTarget.LabelShort} /c[{TC.Yellow}]consumed the /c[{TC.Item}]{PotionLabel}";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: $"{actualTarget.Label}'s muscles surge with power",
            alertColor: Color.Gold
        );
    }

    public override string GetEffectDescription()
    {
        return "+2 Strength";
    }
}
