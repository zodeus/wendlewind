using Grafted.Maths;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Zones;

public class ZoneResourceRecord {
    public ItemDef Item = null!;
    public RangeInt Amount;
    public float ChanceToHarvest = 1;
    public RangeFloat HarvestArea;
}