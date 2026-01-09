namespace Grafted.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Spiced Churni - applies regeneration to all body parts.
/// </summary>
[UsedImplicitly]
public class SpicedChurniHandler : PotionHandler
{
    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;
    public override bool CanAutoUse => true;
    
    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        var actualTarget = target ?? user;
        var duration = GetDuration();

        // Apply regeneration to all body parts
        actualTarget.Body.AllParts.ForEach(p => p.TryAddModifier(
            BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.PurpleRegeneration, duration)
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
    
    public override PotionUseResult? TryAutoUse(Pawn pawn)
    {
        var externalParts = pawn.Body.AllExternalParts;
        
        // Check if both eyes are destroyed
        var eyes = externalParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count > 0 && eyes.All(e => !e.IsFunctional))
            return UseInCombat(pawn);
        
        // Check if 50% of external parts are below 50% health
        var damagedCount = externalParts.Count(p => p.HealthPercent < 0.6);
        if (damagedCount >= externalParts.Count * 0.4)
            return UseInCombat(pawn);
        
        return null;
    }
    
    public override string GetEffectDescription()
    {
        return "Applies regeneration to all body parts.";
    }
}
