namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Grants a temporary Strength bonus. Power is read from the potion's PotionPower stat.
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
        var power = GetStatValue(Defs.Stats.PotionPower);

        actualTarget.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Strengthened,
            TicksLeft = duration,
            Power = power
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
        var power = GetStatValue(Defs.Stats.PotionPower);
        return $"+{power:0.##} Strength";
    }
}
