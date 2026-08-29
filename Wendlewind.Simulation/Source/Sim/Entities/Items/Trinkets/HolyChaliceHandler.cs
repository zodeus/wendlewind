namespace Wendlewind.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class HolyChaliceHandler : TrinketHandler
{
    public HolyChaliceHandler(IRng rng)
    {
        Rng = rng;
    }

    public float CurrentOffing;
    public float CurrentOffingPercentage => CurrentOffing / 1000;

    public override void PostCombatAction(PostCombatReport postCombatReport)
    {
        CurrentOffing = postCombatReport.Enemy.Body.MaxBlood - postCombatReport.Enemy.Body.BloodAmount;
    }

    public override void Tick()
    {
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref CurrentOffing, "CurrentOffing");
        base.ExposeData();
    }
}