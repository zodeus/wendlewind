namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class SteroidInjectorHandler : TrinketHandler
{
    public double FuelLevel;

    private static readonly SimpleCurve CostCurve =
    [
        new CurvePoint(0, 50),
        new CurvePoint(1, 50),

        new CurvePoint(1, 100),
        new CurvePoint(2, 100),

        new CurvePoint(2, 200),
        new CurvePoint(5, 200),

        new CurvePoint(5, 300),
        new CurvePoint(10, 300),

        new CurvePoint(10, 500),
        new CurvePoint(20, 500),

        new CurvePoint(20, 800),
        new CurvePoint(40, 800),

        new CurvePoint(40, 1000),
        new CurvePoint(50, 1000),

        new CurvePoint(50, 2000),
        new CurvePoint(100, 2000),
    ];

    public override void PostCombatAction(PostCombatReport postCombatReport)
    {
        FuelLevel = postCombatReport.TotalDirectPlayerDamage;
    }

    public void InjectPart(BodyPart bodyPart)
    {
        var cost = CalculateTotalCost(bodyPart);
        FuelLevel -= cost;
        InjectPartInternal(bodyPart);
        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            InjectPartInternal(internalPart);
        }
        Core.Context.Achievements.OnItemUsed(Core.Context.Player.Pawn, Trinket, new { Amount = cost, BodyPart = bodyPart });
    }

    private void InjectPartInternal(BodyPart bodyPart)
    {
        bodyPart.MaxHitPoints += CalculateHpValue(bodyPart);
    }

    private static float CalculateHpValue(BodyPart bodyPart)
    {
        return (float)Math.Clamp(bodyPart.MaxHitPoints * .1, 1, 9999999);
    }

    public float CalculateCost(BodyPart bodyPart)
    {
        var hpValue = CalculateHpValue(bodyPart);
        var cost = CostCurve.Evaluate(hpValue);

        if (bodyPart.IsVital)
        {
            cost *= 3;
        }

        switch (bodyPart.Type)
        {
            case BodyPartType.Eye:
                cost *= 5;
                break;
            case BodyPartType.Brain:
                cost *= 4;
                break;
            case BodyPartType.Heart:
                cost *= 4;
                break;
        }

        return cost;
    }

    public float CalculateTotalCost(BodyPart bodyPart)
    {
        return CalculateCost(bodyPart) + bodyPart.AllInternalParts.Sum(CalculateCost);
    }

    public bool HasFuelFor(BodyPart bodyPart)
    {
        return CalculateTotalCost(bodyPart) <= FuelLevel;
    }

    public override void Tick()
    {
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref FuelLevel, "FuelLevel");
        base.ExposeData();
    }
}