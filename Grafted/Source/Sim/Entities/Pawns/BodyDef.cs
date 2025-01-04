using Grafted.Sim.Entities.Pawns.Bodies.Handlers;

namespace Grafted.Sim.Entities.Pawns;

public class BodyDef : Def {
    public BloodDef BloodType = null!;
    public float MaxBlood = 5000;
    public float MaxEnergy = 100;
    public float BoneDensity = 1;
    public Type GeneratorClass = typeof(IBodyGenerator);
    public Type HandlerClass = typeof(DefaultBodyHandler);
    
    private IBodyGenerator? _generator;
    public DefaultBodyHandler Handler => (DefaultBodyHandler) Activator.CreateInstance(HandlerClass)!;
    public IBodyGenerator Generator => _generator ??= (IBodyGenerator) Activator.CreateInstance(GeneratorClass)!;
}