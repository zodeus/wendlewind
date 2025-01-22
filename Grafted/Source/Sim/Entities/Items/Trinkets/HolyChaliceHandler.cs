namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class HolyChaliceHandler : TrinketHandler
{
    public float CurrentOffing;
    public float CurrentOffingPercentage => CurrentOffing / 10000;

    public override void PostCombatAction(Pawn playerPawn, Pawn enemyPawn)
    {
        CurrentOffing = enemyPawn.Body.MaxBlood - enemyPawn.Body.BloodAmount;
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