namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class SteroidInjectorHandler : TrinketHandler
{
    public double TotalDamage;

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
        var range = new RangeFloat(0.8f, 1.2f);
        TotalDamage = postCombatReport.TotalDirectPlayerDamage;
    }

    public void InjectPart(BodyPart bodyPart)
    {
        bodyPart.MaxHitPoints += CalculateHpValue(bodyPart);
        TotalDamage -= CalculateCost(bodyPart);
        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            InjectPart(internalPart);
        }
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
        foreach (var internalPart in bodyPart.AllInternalParts)
        {
            Log.Info($"  {internalPart.Label}: {CalculateHpValue(internalPart)}  {CalculateCost(internalPart)}");
        }

        return CalculateCost(bodyPart) + bodyPart.AllInternalParts.Sum(CalculateCost);
    }

    public bool HasFuelFor(BodyPart bodyPart)
    {
        return CalculateTotalCost(bodyPart) <= TotalDamage;
    }

    public override void Tick()
    {
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref TotalDamage, "TotalDamage");
        base.ExposeData();
    }
}