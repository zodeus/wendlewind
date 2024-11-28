using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

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
    public EntityContainer? Container;

    public virtual string Label => Def.Label;
    public virtual string LabelShort => Def.Label;

    public virtual string Description => Def.Description;
    public virtual Texture2D Icon => Def.Icon;
    public bool IsDestroyed => _internalState == EntityState.Destroyed;

    public virtual void Destroy() {
        _internalState = EntityState.Destroyed;
    }

    public virtual void Initialize() { }

    public virtual void Tick(int ticks) { }

    public override int GetHashCode() {
        return Id;
    }

    public virtual void ExposeData() {
        ScribeValues.Look(ref Id, "Id");
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref _internalState, "InternalState");
    }

    public string GetUniqueId() {
        return "Entity_" + Id;
    }

    public override string ToString() {
        return Label;
    }

    public override bool Equals(object? obj) {
        return ((Entity?) obj)?.Id == Id;
    }

    public EntityPanelBase UiPanel(BaseGui gui, EntityPanelProperties? properties = null) {
        return Def.UiPanelFor(gui, this, properties);
    }

    public virtual void Render(SpriteBatch spriteBatch, float deltaTime) { }
}