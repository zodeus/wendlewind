using Grafted.Sim.Entities;
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
            TownGenerator.PopulateMerchantContainer(this);
        }

        Entities.Tick();
    }
}