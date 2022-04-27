using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim;

public class TownGenerator {
    public static Town Generate(ZoneDef zoneDef) {
        return new Town {
            ZoneDef = zoneDef,
            Merchant = GenerateMerchant(zoneDef)
        };
    }

    public static TownMerchant GenerateMerchant(ZoneDef zoneDef) {
        ItemContainer container = new();
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.MendersMist, Core.Random.Next(50, 100)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.MedKit, Core.Random.Next(20, 50)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.ArterialThreads, Core.Random.Next(100, 200)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.JarOfBlood, Core.Random.Next(5, 20)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.PumpinJuice, Core.Random.Next(5, 20)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.AcidFlask, Core.Random.Next(5, 20)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.ShortSword, Core.Random.Next(1, 1)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.RepairKit, Core.Random.Next(20, 50)));
        container.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.SoulCoin, Core.Random.Next(50, 100)));

        return new TownMerchant {
            Items = container
        };
    }
}