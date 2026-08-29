namespace Wendlewind.Sim.Entities;

public static class EntityGenerator {
    public static T CreateEntity<T>(ItemDef def, int stackSize) where T : Item {
        T entity = CreateEntity<T>(def);
        if (entity.IsStackable == false && stackSize != 1) {
            Log.Error($"Tried to create entity with StackSize of {stackSize} but {entity} is not stackable, setting StackSize to 1");
            entity.StackSize = 1;
        }
        else if (stackSize > def.StackLimit) {
            Log.Error($"Tried to create entity with StackSize of {stackSize} but {entity} StackLimit is {def.StackLimit}, setting StackSize to {def.StackLimit}");
            entity.StackSize = def.StackLimit;
        }
        else {
            entity.StackSize = stackSize;
        }

        return entity;
    }

    public static T CreateEntity<T>(EntityDef def, bool suppressInitialization = false /*, EntityDef material = null*/) where T : Entity {
        return (T) CreateEntity(def, suppressInitialization);
    }

    private static Entity CreateEntity(EntityDef def, bool suppressInitialization = false /*, EntityDef material = null*/) {
        Entity entity = (Entity) Activator.CreateInstance(def.EntityClass)!;
        entity.Id = Core.Context.IdProvider.NextEntityId();
        entity.Def = def;
        /*entity.SetBaseMaterialDirect(material);*/
        if (suppressInitialization == false) {
            entity.Initialize();
        }

        return entity;
    }
}