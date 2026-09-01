namespace Wendlemire.Sim.Entities;

public static class EntityGenerator
{
    public static T CreateEntity<T>(GameContext context, ItemDef def, int stackSize) where T : Item =>
        context.Factory.CreateEntity<T>(def, stackSize);

    public static T CreateEntity<T>(GameContext context, EntityDef def, bool suppressInitialization = false) where T : Entity =>
        context.Factory.CreateEntity<T>(def, suppressInitialization);
}
