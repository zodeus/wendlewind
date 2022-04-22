using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Persistence;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Entities;

public enum EntityState {
    Spawned,
    UnSpawned,
    Destroyed
}

public abstract class Entity : IExposable, IIdentityProvider {
    private EntityState _internalState = EntityState.UnSpawned;

    public int Id = -1;
    public EntityDef Def = null!;
    public ItemContainer? Container;
    
    public virtual string Label => Def.Label;
    public virtual string LabelShort => Def.Label;

    public virtual string Description => Def.Description;
    public virtual Texture2D Icon => Def.Icon;
    public bool IsDestroyed => _internalState == EntityState.Destroyed;

    public virtual void Destroy() {
        _internalState = EntityState.Destroyed;
    }

    public virtual void Initialize() { }

    public virtual void Tick() { }

    public override int GetHashCode() {
        return Id;
    }

    public virtual void ExposeData() { }

    public string GetUniqueId() {
        return "Entity_" + Id;
    }

    public override string ToString() {
        return Label;
    }

    public override bool Equals(object? obj) {
        return ((Entity?) obj)?.Id == Id;
    }

    public EntityPanelBase UiPanel(EntityPanelProperties? properties = null) {
        return Def.UiPanelFor(this, properties);
    }
}