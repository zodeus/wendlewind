namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Applies regeneration to all body parts and the Strengthened body effect.
/// Power is read from the potion's PotionPower stat.
/// </summary>
[UsedImplicitly]
public class HealingPotionHandler : PotionHandler
{
    public HealingPotionHandler(IRng rng)
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

        actualTarget.Body.AllParts.ForEach(p => p.TryAddModifier(
            Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, duration, power)
        ));

        actualTarget.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Strengthened,
            TicksLeft = duration
        });

        var message = $"/c[{TC.Attacker}]{actualTarget.LabelShort} /c[{TC.Yellow}]drank the /c[{TC.Item}]{PotionLabel}";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: $"{actualTarget.Label} is mending",
            alertColor: Color.GreenYellow
        );
    }

    public override string GetEffectDescription()
    {
        var power = GetStatValue(Defs.Stats.PotionPower);
        return $"Applies regeneration ({power:0.##}x) to all body parts.";
    }
}
