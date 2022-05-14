using Grafted.Sim.Persistence;

namespace Grafted.Sim.SpecialEvents;

public abstract class SpecialEventHandler : IExposable {
    public abstract void ExposeData();
}