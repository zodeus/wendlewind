namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class TargetingImpHandler : TrinketHandler
{
    public int AttacksMissed;

    public override void PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (victim.PawnType == PawnType.Enemy && request.TargetedPart != null && response.Missed)
        {
            AttacksMissed++;
            if (AttacksMissed % 5 == 0)
            {
                Charges++;
            }
        }
        base.PostAttackHandler(victim, request, response);
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref AttacksMissed, "AttacksMissed");
        base.ExposeData();
    }
}