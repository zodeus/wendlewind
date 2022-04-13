using Grafted.Maths;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities;

public class ItemDropCount {
    public ItemDef Item = null!;
    public RangeInt Amount;
    public float ChanceToDrop;
}