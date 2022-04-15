using System;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.UI;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnInventoryPanel : Grid {
    private readonly EntityListPanel _entitiesPanel;

    public PawnInventoryPanel(PawnInventory inventory, Action<Entity>? leftClickAction = null) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(30);
        RowSpacing = 10;
        DefaultColumnProportion = Proportion.Auto;
        RowsProportions.Add(Proportion.Auto);
        RowsProportions.Add(Proportion.Fill);
        RowsProportions.Add(Proportion.Auto);
        _entitiesPanel = new EntityListPanel(inventory.Items, leftClickAction: leftClickAction) {
            GridRow = 1, GridColumn = 0, VerticalAlignment = VerticalAlignment.Top
        };
        AddChild(new Label(BaseContent.Styles.Label.Medium) { GridRow = 0, Text = "Inventory" });
        AddChild(_entitiesPanel);
        ImageButton trash = new(BaseContent.Styles.Button.Icon) {
            Width = 48, Height = 48, Padding = new Thickness(6),
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Trash], GridColumn = 0, GridRow = 2
        };
        trash.Click += (_, _) => {
            Mouse.GetState();
            if (Input.LeftMouseButtonReleased && Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
                inventory.Items.Remove(item);
                item.Destroy();
                Core.Sim.Gui!.MouseAttachment.Detach();
            }
        };
        AddChild(trash);
    }


    public void Update() {
        _entitiesPanel.Update();
    }
}