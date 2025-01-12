using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class LootPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly ItemContainerPanel _otherContainerPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;

    public LootPanel(BaseGui gui, Pawn playerPawn, EntityContainer lootContainer)
    {
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities,
            lootContainer
        )
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            Visible = !playerPawn.IsDead, MinHeight = 700, Width = 700
        };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);
        _otherContainerPanel = new ItemContainerPanel(gui, lootContainer, playerPawn.Inventory.Entities)
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Loot], VerticalAlignment = VerticalAlignment.Stretch,
        };

        VerticalStackPanel rightColumn = new()
        {
            Margin = new Thickness(20, 0, 0, 0),
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.AddChild(_equipmentPanel);
        rightColumn.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Ground Loot", Margin = new Thickness(0, 50, 0, 0), });
        rightColumn.AddChild(_otherContainerPanel);
        HorizontalStackPanel grid = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                _bodyPanel,
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Proportions = { Proportion.Auto, Proportion.Fill },
                    Widgets =
                    {
                        new TrinketBar(gui, playerPawn.Inventory.Entities),
                        _inventoryPanel
                    }
                },
                rightColumn
            }
        };
        AddChild(grid);
    }

    public void Update()
    {
        _bodyPanel.Update();
        _otherContainerPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }
}