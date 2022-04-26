using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.TownWidgets;

public class PawnDetailPanel : Panel, IUpdatable {
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly ItemContainerPanel _otherContainerPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;

    public PawnDetailPanel(Pawn playerPawn, string receivingContainerTitle, ItemContainer receivingContainer) {
        _bodyPanel = new PawnBodyPanel(playerPawn.Body) {
            GridRow = 1, GridColumn = 0
        };

        _inventoryPanel = new ItemContainerPanel(
            playerPawn.Inventory.Items,
            receivingContainer
        ) {
            Visible = !playerPawn.IsDead, MinHeight = 700,
            Width = 300, GridRow = 1, GridColumn = 1
        };

        _equipmentPanel = new PawnEquipmentPanel(playerPawn);
        _otherContainerPanel = new ItemContainerPanel(receivingContainer, playerPawn.Inventory.Items) {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame], VerticalAlignment = VerticalAlignment.Stretch
        };

        VerticalStackPanel rightColumn = new() {
            Visible = !playerPawn.IsDead, GridRow = 1, GridColumn = 2,
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.AddChild(_equipmentPanel);
        rightColumn.AddChild(new HorizontalSeparator() { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = receivingContainerTitle });
        rightColumn.AddChild(_otherContainerPanel);
        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center,
            GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                _bodyPanel,
                _inventoryPanel,
                rightColumn
            }
        };
        AddChild(grid);
    }

    public void Update() {
        _bodyPanel.Update();
        _otherContainerPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }
}