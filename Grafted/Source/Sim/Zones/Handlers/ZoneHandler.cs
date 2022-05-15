using Grafted.Sim.Persistence;

namespace Grafted.Sim.Zones.Handlers;

public abstract class ZoneHandler : IExposable {
    public Zone Zone = null!;
    public World World = null!;

    public virtual void ExposeData() {
        Scribe_References.Look(ref Zone!, "Zone");
    }

    public virtual void Initialize(World world, Zone zone) {
        World = world;
        Zone = zone;
    }

    public virtual void Tick() { }
    public abstract void OnEnter();
    public abstract void OnExit();
}