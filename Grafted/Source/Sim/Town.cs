using Grafted.Sim.Entities;

namespace Grafted.Sim;

public class Town {
    public ItemContainer Storage = new();
    public TownMerchant Merchant = null!;
    public ZoneDef ZoneDef = null!;
}

public class TownMerchant {
    public ItemContainer Items = null!;
}