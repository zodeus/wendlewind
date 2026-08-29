using Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

namespace Wendlewind.Sim.Entities.Pawns;

public class BodyDef : Def {
    public BloodDef? BloodType;
    public float MaxBlood = 0;
    public float MaxEnergy = 100;
    public float BoneDensity = 1;
    public Type GeneratorClass = typeof(IBodyGenerator);
    public Type HandlerClass = typeof(DefaultBodyHandler);
    public Type? LayoutClass;

    public DefaultBodyHandler CreateHandler(ISimFactory factory) => factory.Create<DefaultBodyHandler>(HandlerClass);
    public IBodyGenerator CreateGenerator(ISimFactory factory) => factory.Create<IBodyGenerator>(GeneratorClass);
}
