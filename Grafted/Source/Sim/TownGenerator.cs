using System;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Zones.Handlers;
using Grafted.Utils;

namespace Grafted.Sim;

public static class TownGenerator {
    public static TownStructure GenerateStructure(TownStructureDef def, Town town) {
        TownStructure structure = (TownStructure) Activator.CreateInstance(def.StructureClass)!;
        structure.Def = def;
        structure.Id = Core.Sim.IdProvider.NextWorldObjectId();
        structure.Town = town;
        structure.Initialize();
        return structure;
    }

    public static void PopulateMerchantContainer(TownStructureMerchant structureMerchant) {
        structureMerchant.Entities.Clear();
        int maxTier = 1;
        if (Core.Sim.World.Zones[Defs.Zones.PeacefulMeadow].IsComplete) {
            maxTier = 2;
        }

        if (Core.Sim.World.Zones[Defs.Zones.TheOutskirts].IsComplete) {
            maxTier = 3;
        }

        if (Core.Sim.World.Zones[Defs.Zones.GrainMill].IsComplete) {
            maxTier = 4;
        }

        if (Core.Sim.World.Zones[Defs.Zones.FesterpusSwamp].IsComplete) {
            maxTier = 5;
        }

        var medicalItems = DefRepository<ItemDef>.Defs.Where(
            def => def.ItemType == ItemType.Medical && def.BaseStats.GetStatValueFromList(Defs.Stats.Tier) <= maxTier
        ).InRandomOrder().Take(Core.Random.Next(3, 5));
        foreach (ItemDef def in medicalItems) {
            float? currency = def.BaseStats.GetStatValueFromList(Defs.Stats.CurrencyValue);
            if (currency is null or <= 0) {
                continue;
            }

            structureMerchant.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(4, 8)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.TradeTool && def.BaseStats.GetStatValueFromList(Defs.Stats.Tier) <= maxTier).InRandomOrder().Take(4)) {
            structureMerchant.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(6, 10)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.Potion && def.BaseStats.GetStatValueFromList(Defs.Stats.Tier) <= maxTier).InRandomOrder().Take(2)) {
            structureMerchant.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(2, 3)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.Resource && def.BaseStats.GetStatValueFromList(Defs.Stats.Tier) <= maxTier).InRandomOrder().Take(1)) {
            structureMerchant.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(1, 3)));
        }

        var equipment = DefRepository<ItemDef>.Defs.Where(
            def => def.ItemType == ItemType.Equipment && def.BaseStats.GetStatValueFromList(Defs.Stats.CurrencyValue) > 0 && def.BaseStats.GetStatValueFromList(Defs.Stats.Tier) <= maxTier
        ).InRandomOrder().Take(8);
        foreach (ItemDef def in equipment) {
            //for (int i = 0; i < Core.Random.Next(1, 3); i++) {
            structureMerchant.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(def));
            //}
        }

        structureMerchant.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.Coin, Core.Random.Next(1000, 2000) * maxTier));
    }
}