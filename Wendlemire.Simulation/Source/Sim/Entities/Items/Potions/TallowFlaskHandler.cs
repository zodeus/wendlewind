namespace Wendlemire.Sim.Entities.Items.Potions;

/// <summary>
/// Thrown flask of hot fat that gums the target's legs, cutting mobility and attack speed.
/// </summary>
[UsedImplicitly]
public class TallowFlaskHandler : PotionHandler
{
    public TallowFlaskHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool CanUseInCombat => true;
    public override bool CanUseOutsideCombat => false;

    public override Pawn GetCombatApplicationTarget(Pawn user, Pawn? opponent) => opponent ?? user;

    public override PotionUseResult UseInCombat(Pawn user, Pawn? target = null)
    {
        if (target == null)
        {
            return PotionUseResult.Failed("Tallow Flask requires a target.");
        }

        var duration = GetDuration();
        target.Body.Effects.TryApplyEffect(new BodyEffect
        {
            Def = Defs.BodyEffects.Tallowed,
            TicksLeft = duration
        });

        var message = $"/c[{TC.Attacker}]{user.LabelShort} /c[{TC.Yellow}]smashed a /c[{TC.Item}]{PotionLabel} /c[{TC.Yellow}]at /c[{TC.Victim}]{target.LabelShort}'s /c[{TC.Yellow}]feet";

        return PotionUseResult.Succeeded(
            message,
            alertMessage: $"{target.Label}'s legs are gummed with tallow",
            alertColor: Color.Goldenrod
        );
    }

    public override string GetEffectDescription()
    {
        return "Smashes underfoot and gums the target's legs, slowing mobility and strikes.";
    }
}
