using System;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;

namespace Grafted.Sim;

public static class TownGenerator {
    public static Town Generate(ZoneDef zoneDef) {
        Town town = new() {
            ZoneDef = zoneDef
        };
        foreach (TownStructureDef structureDef in DefRepository<TownStructureDef>.Defs) {
            town.AddStructure(GenerateStructure(structureDef, town));
        }

        return town;
    }

    public static TownStructure GenerateStructure(TownStructureDef def, Town town) {
        TownStructure structure = (TownStructure) Activator.CreateInstance(def.StructureClass)!;
        structure.Def = def;
        structure.Id = Core.Sim.IdProvider.NextWorldObjectId();
        structure.Town = town;
        structure.Initialize();
        return structure;
    }

    public static void PopulateMerchantContainer(TownStructureMerchant structureMerchant) {
        structureMerchant.Items.Clear();
        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.Medical).InRandomOrder().Take(Core.Random.Next(4, 5))) {
            float? currency = def.BaseStats.GetStatValueFromList(Defs.Stats.CurrencyValue);
            if (currency is null or <= 0) {
                continue;
            }

            structureMerchant.Items.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(4, 13)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.TradeTool).InRandomOrder().Take(4)) {
            structureMerchant.Items.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(2, 8)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.Potion).InRandomOrder().Take(2)) {
            structureMerchant.Items.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(1, 5)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.Resource).InRandomOrder().Take(1)) {
            structureMerchant.Items.TryAdd(EntityGenerator.CreateEntity<Item>(def, Core.Random.Next(3, 13)));
        }

        foreach (ItemDef def in DefRepository<ItemDef>.Defs.Where(def => def.ItemType == ItemType.Equipment && def.BaseStats.GetStatValueFromList(Defs.Stats.CurrencyValue) > 0).InRandomOrder().Take(Core.Random.Next(6, 9))) {
            //for (int i = 0; i < Core.Random.Next(1, 3); i++) {
            structureMerchant.Items.TryAdd(EntityGenerator.CreateEntity<Item>(def));
            //}
        }

        structureMerchant.Items.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.SoulCoin, Core.Random.Next(199, 699)));
    }
}