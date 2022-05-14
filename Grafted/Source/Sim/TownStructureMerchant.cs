using Grafted.Sim.Entities;
using Grafted.Sim.Persistence;
using JetBrains.Annotations;

namespace Grafted.Sim;

[UsedImplicitly]
public class TownStructureMerchant : TownStructure {
    private int _lastRefreshTick = 0;
    private int _refreshInterval = SimTime.HoursToTicks(24);
    public EntityContainer Entities = null!;

    public override void Initialize() {
        Entities = new EntityContainer(9999);
    }

    public override void Tick() {
        if (_lastRefreshTick == 0 || _lastRefreshTick + _refreshInterval <= Core.Sim.Ticks) {
            _lastRefreshTick = Core.Sim.Ticks;
            Restock();
        }

        Entities.Tick();
    }

    public void Restock() {
        TownGenerator.PopulateMerchantContainer(this);
    }

    public override void ExposeData() {
        Scribe_Values.Look(ref _lastRefreshTick!, "_lastRefreshTick");
        Scribe_Values.Look(ref _refreshInterval!, "_refreshInterval");
        Scribe_Deep.Look(ref Entities!, "Entities");
        base.ExposeData();
    }
}