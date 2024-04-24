using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnDetailPanel : Panel, IUpdatable {
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly ItemContainerPanel _otherContainerPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;

    public PawnDetailPanel(BaseGui gui, Pawn playerPawn, string receivingContainerTitle, EntityContainer receivingContainer) {
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities,
            receivingContainer
        ) { Visible = !playerPawn.IsDead, MinHeight = 700, Width = 700 };

        _equipmentPanel = new PawnEquipmentPanel(gui,playerPawn);
        _otherContainerPanel = new ItemContainerPanel(gui,receivingContainer, playerPawn.Inventory.Entities) {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame], VerticalAlignment = VerticalAlignment.Stretch
        };

        VerticalStackPanel rightColumn = new() {
            Visible = !playerPawn.IsDead, 
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.AddChild(_equipmentPanel);
        rightColumn.AddChild(new HorizontalSeparator { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = receivingContainerTitle });
        rightColumn.AddChild(_otherContainerPanel);
        HorizontalStackPanel grid = new() {
            HorizontalAlignment = HorizontalAlignment.Center,
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