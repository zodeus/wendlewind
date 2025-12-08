using Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;
using Grafted.Sim.Entities.Pawns.Bodies.Handlers;

namespace Grafted.Sim.Entities.Pawns;

public class BodyDef : Def {
    public BloodDef? BloodType;
    public float MaxBlood = 0;
    public float MaxEnergy = 100;
    public float BoneDensity = 1;
    public Type GeneratorClass = typeof(IBodyGenerator);
    public Type HandlerClass = typeof(DefaultBodyHandler);
    public Type? LayoutClass;
    
    private IBodyGenerator? _generator;
    private IBodyPartLayout? _layout;
    
    public DefaultBodyHandler Handler => (DefaultBodyHandler) Activator.CreateInstance(HandlerClass)!;
    public IBodyGenerator Generator => _generator ??= (IBodyGenerator) Activator.CreateInstance(GeneratorClass)!;
    public IBodyPartLayout? Layout => LayoutClass != null 
        ? _layout ??= (IBodyPartLayout) Activator.CreateInstance(LayoutClass)! 
        : null;
}