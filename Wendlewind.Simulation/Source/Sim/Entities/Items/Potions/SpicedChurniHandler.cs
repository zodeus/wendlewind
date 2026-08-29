namespace Wendlewind.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Spiced Churni - applies regeneration to all body parts.
/// </summary>
[UsedImplicitly]
public class SpicedChurniHandler : PotionHandler
{
    public SpicedChurniHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;
    
    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        var actualTarget = user;
        var duration = GetDuration();

        // Apply regeneration to all body parts
        actualTarget.Body.AllParts.ForEach(p => p.TryAddModifier(
            Context.Factory.CreateModifier(Defs.BodyPartModifiers.HealthRegeneration, duration, 1)
        ));

        // Apply the body effect
        actualTarget.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.FeelingThePurple,
            TicksLeft = duration
        });

        var message = $"/c[{TC.Attacker}]{actualTarget.LabelShort} /c[{TC.Yellow}]sipped the /c[{TC.Item}]{PotionLabel}";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: $"{actualTarget.Label} is absorbing the spices",
            alertColor: Color.GreenYellow
        );
    }
    
    public override string GetEffectDescription()
    {
        return "Applies regeneration to all body parts.";
    }
}
