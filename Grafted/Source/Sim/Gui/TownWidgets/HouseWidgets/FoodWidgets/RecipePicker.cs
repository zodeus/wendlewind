using System;
using System.Collections.Generic;
using Grafted.Sim.Entities.Items;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.TownWidgets.HouseWidgets.FoodWidgets;

public class RecipePicker : ComboBox {
    public RecipePicker(List<ItemDef> defs, EventHandler changeAction) {
        ListItem unselectedItem = new() { Text = "Pick a recipe" };
        base.Items.Add(unselectedItem);
        base.SelectedItem = unselectedItem;
        foreach (ItemDef def in defs) {
            ListItem comboItem = new() { Text = def.Label, Tag = def };
            base.Items.Add(comboItem);
        }

        base.SelectedIndexChanged += changeAction;
    }
}