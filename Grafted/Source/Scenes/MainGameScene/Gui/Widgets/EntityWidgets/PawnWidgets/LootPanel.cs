using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class LootPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly ItemContainerPanel? _otherContainerPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnBodyEffectsPanel _pawnEffectsPanel;

    public LootPanel(BaseGui gui, Pawn playerPawn, EntityContainer? lootContainer)
    {
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn)
        {
            Padding = new Thickness(15)
        };
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui, playerPawn.Inventory.Entities, lootContainer)
        {
            MinHeight = 400,
            MaxHeight = 600,
            Width = 720,
            Visible = !playerPawn.IsDead
        };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);
        if (lootContainer != null)
        {
            _otherContainerPanel = new ItemContainerPanel(gui, lootContainer, playerPawn.Inventory.Entities)
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Loot], VerticalAlignment = VerticalAlignment.Stretch,
            };
        }

        var lootTitleLabel = new Label(BaseContent.Styles.Label.Medium) { Text = "Loot" };
        VerticalStackPanel rightColumn = new()
        {
            Spacing = 15,
            Margin = new Thickness(20, 0, 0, 0),
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(_pawnEffectsPanel);
        if (_otherContainerPanel != null)
        {
            rightColumn.Widgets.Add(lootTitleLabel);
            rightColumn.Widgets.Add(_otherContainerPanel);
        }

        HorizontalStackPanel grid = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                _bodyPanel,
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Widgets =
                    {
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.Combat, item => gui.ViewEntity(item), false) { TrinketsPerRow = 9 },
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.Passive, item => gui.ViewEntity(item), false) { TrinketsPerRow = 9 },
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.Interactive, item => gui.ViewEntity(item), false) { TrinketsPerRow = 9 },
                        _inventoryPanel
                    }
                },
                rightColumn
            }
        };
        Widgets.Add(grid);
    }

    public void Update()
    {
        _bodyPanel.Update();
        _otherContainerPanel?.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
        _pawnEffectsPanel.Update();
    }
}