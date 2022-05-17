using System;
using System.Collections.Generic;
using Grafted.Sim.Entities.Items;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.TownWidgets.HouseWidgets.FoodWidgets;

public class RecipePicker : ComboBox {
    public RecipePicker(List<ItemDef> defs, EventHandler changeAction) {
        ListItem unselectedItem = new() { Text = "Pick a recipe" };
        base.Items.Add(unselectedItem);
        base.SelectedItem = unselectedItem;
        foreach (ItemDef def in defs) {
            ListItem comboItem = new() { Image = new TextureRegion(def.Icon,new Rectangle(0,0,16,16)), Text = def.Label, Tag = def,  };
            base.Items.Add(comboItem);
        }

        base.SelectedIndexChanged += changeAction;
    }
}