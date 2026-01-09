namespace Grafted.Sim.Entities.Items.Potions;

/// <summary>
/// Handler for Jar of Blood potion - restores blood to maximum.
/// </summary>
[UsedImplicitly]
public class JarOfBloodHandler : PotionHandler
{
    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => true;
    public override bool CanAutoUse => true;
    
    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        return RestoreBlood(user);
    }
    
    public override PotionUseResult UseOutsideCombat(Pawn user)
    {
        return RestoreBlood(user);
    }
    
    private PotionUseResult RestoreBlood(Pawn pawn)
    {
        pawn.Body.BloodAmount = pawn.Body.MaxBlood;
        
        var message = $"/c[{TC.Attacker}]{pawn.LabelShort} /c[{TC.Yellow}]sipped a /c[{TC.Item}]{PotionLabel}";
        
        return PotionUseResult.Succeeded(
            message,
            alertMessage: "Sipped a Jar of Blood. Blood is good for battle, bad for the mind",
            alertColor: Color.DarkRed
        );
    }
    
    public override string GetEffectDescription()
    {
        return "Instantly restores all lost blood.";
    }
    
    public override PotionUseResult? TryAutoUse(Pawn pawn)
    {
        if (pawn.Body.BloodPercent < .1f)
            return UseInCombat(pawn);
        return null;
    }
}
