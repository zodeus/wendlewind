using System;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnInventoryPanel : Grid {
    private readonly EntityListPanel _entitiesPanel;

    public PawnInventoryPanel(PawnInventory inventory, Action<Entity>? rightClientAction = null) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(30);
        RowSpacing = 10;
        DefaultColumnProportion = Proportion.Auto;
        DefaultRowProportion = Proportion.Auto;

        _entitiesPanel = new EntityListPanel(inventory.Items, rightClickAction: rightClientAction) {
            GridRow = 1, GridColumn = 0, VerticalAlignment = VerticalAlignment.Top
        };
        AddChild(new Label(BaseContent.Styles.Label.Medium) { GridRow = 0, Text = "Inventory" });
        AddChild(_entitiesPanel);
    }


    public void Update() {
        _entitiesPanel.Update();
    }
}