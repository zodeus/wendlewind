using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items.Medicinals;
using Grafted.Sim.Gui.Widgets.DefWidgets;

namespace Grafted.Sim.Entities.Items;

public class ItemDef : EntityDef {
    public override EntityType EntityType => EntityType.Item;
    public override Type DefUiClass => typeof(ItemDefPanel);

    public ItemType ItemType = ItemType.None;
    public ToolType ToolType = ToolType.None;
    public List<ToolCategory> ToolCategories = new();
    public int StackLimit = 1;
    public EquipmentProperties EquipmentProperties = new();
    public WeaponProperties WeaponProperties = new();
    public List<ToolSequenceDef> ToolSequences = new();

    public CraftingProperties CraftingProperties = new();
    public FoodProperties? FoodProperties;
    public MedicinalProperties? MedicinalProperties;

    public override void ResolveDependencies() {
        base.ResolveDependencies();
        if (ToolType == ToolType.None) {
            return;
        }

        if (ToolSequences.Any()) {
            Log.Info($"Sequences for Def:{Moniker} have been specified by XML, skipping auto-associations");
            return;
        }

        foreach (var toolSequenceDef in DefRepository<ToolSequenceDef>.Defs.Where(def => def.Maneuvers.Any(maneuverDef => maneuverDef.Tools?.Contains(ToolType) ?? false))) {
            ToolSequences.Add(toolSequenceDef);
        }
    }
}