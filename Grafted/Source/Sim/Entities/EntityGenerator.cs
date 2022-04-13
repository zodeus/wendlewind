using System;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities;

public static class EntityGenerator {
    public static T CreateEntity<T>(ItemDef def, int stackSize) where T : Item {
        T entity = CreateEntity<T>(def);
        entity.StackSize = stackSize;
        return entity;
    }

    public static T CreateEntity<T>(EntityDef def, bool suppressInitialization = false /*, EntityDef material = null*/) where T : Entity {
        return (T) CreateEntity(def, suppressInitialization);
    }

    private static Entity CreateEntity(EntityDef def, bool suppressInitialization = false /*, EntityDef material = null*/) {
        Entity entity = (Entity) Activator.CreateInstance(def.EntityClass)!;
        entity.Id = Core.Sim.IdProvider.NextEntityId();
        entity.Def = def;
        /*entity.SetBaseMaterialDirect(material);*/
        if (suppressInitialization == false) {
            entity.Initialize();
        }

        return entity;
    }
}