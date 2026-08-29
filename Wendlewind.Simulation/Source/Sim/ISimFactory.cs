namespace Wendlewind.Sim;

public interface ISimFactory : IScribeObjectFactory
{
    T Create<T>(Type type) where T : class;
    T Create<T>(Type type, params object[] ctorArgs) where T : class;
    T CreateEntity<T>(EntityDef def, bool suppressInitialization = false) where T : Entity;
    T CreateEntity<T>(ItemDef def, int stackSize) where T : Item;
    BodyPartModifier CreateModifier(BodyPartModifierDef def, int duration, double power);
    void Bind(object? instance);
    void RebindGraph();
}
