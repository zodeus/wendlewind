using Grafted.Scenes.MainGameScene.Gui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

namespace Grafted.Sim.Entities;

public enum EntityState
{
    Spawned,
    UnSpawned,
    Destroyed
}

public abstract class Entity : IExposable, IIdentityProvider
{
    public event Action<Entity>? Destroyed;//todo - actions
    public event Action<Entity>? EjectedFromContainer; //todo - actions
    
    public int Id = -1;
    public EntityDef Def = null!;
    public virtual string Label => Def.Label;
    public virtual string LabelShort => Def.Label;
    public virtual string Description => Def.Description;
    public virtual Texture2D Icon => Def.Icon;
    public bool IsDestroyed => _internalState == EntityState.Destroyed;
    
    private EntityState _internalState = EntityState.UnSpawned;
    
    public void EjectFromContainer()
    {
        EjectedFromContainer?.Invoke(this);
    }

    public virtual void Destroy()
    {
        _internalState = EntityState.Destroyed;
        Destroyed?.Invoke(this);
    }

    public virtual void Initialize()
    {
    }

    public virtual void Tick()
    {
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public virtual void ExposeData()
    {
        ScribeValues.Look(ref Id, "Id");
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref _internalState, "InternalState");
    }

    public string GetUniqueId()
    {
        return "Entity_" + Id;
    }

    public override string ToString()
    {
        return Label;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity e && e.GetUniqueId() == GetUniqueId();
    }

    public EntityPanelBase UiPanel(BaseGui gui, EntityPanelProperties? properties = null)
    {
        return Def.UiPanelFor(gui, this, properties);
    }

    public virtual void Render(SpriteBatch spriteBatch, float deltaTime)
    {
    }
}