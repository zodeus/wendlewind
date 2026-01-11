namespace Grafted.Sim.Entities.Pawns.Bodies.Handlers;

/// <summary>
/// A body handler for the RustyDoll that allows it to regenerate destroyed parts.
/// When a head or torso is destroyed, a new RustyDoll part is attached with doubled HP.
/// Each regeneration also doubles the pawn's strength.
/// This can happen up to 8 times.
/// </summary>
[UsedImplicitly]
public class RustyDollBodyHandler : DefaultBodyHandler
{
    private const float MaxGenerations = 8;
    private int _generationCount;

    private int _regenerationCooldownTicks = 10;
    
    private double _currentHealthThreshold = 0.9;

    // Track accumulated stat bonuses from regenerations
    public float StrengthMultiplier { get; private set; } = 1f;
    public float ActiveStrengthMultiplier { get; private set; } = 1f;
    public override void Tick()
    {
        _regenerationCooldownTicks--;
        if(Core.Context.Ticks % 120 == 0)
        {
            ActiveStrengthMultiplier = Core.Random.NextFloat(1f, StrengthMultiplier);
        }

        if (Body.EnergyPercent < 0.9f)
        {
            Body.Energy = Body.MaxEnergy;
        } 
        
        if (_generationCount >= MaxGenerations || _regenerationCooldownTicks > 0)
        {
            return;
        }

        var rootPart = Body.RootSocket!.AttachedPart!;
        if (rootPart.HealthPercent >= _currentHealthThreshold)
        {
            return;
        }

        var minionSockets = rootPart.GetSocketsFor(BodyPartType.Minion);

        GenerateMinion(minionSockets[_generationCount]!, _generationCount);
        _regenerationCooldownTicks = 10;
        StrengthMultiplier *= 1.2f;
        _generationCount++;
        _currentHealthThreshold -= .03f;
    }

    private void GenerateMinion(BodyPartSocket socket, int index)
    {
        RustyDollBodyGenerator.GenerateMinion(socket, index + 1);
    }

    public override void ModifyStat(StatDef stat, ref float value)
    {
        if (stat == Defs.Stats.Strength)
        {
            value *= ActiveStrengthMultiplier; 
        }
    }
}
