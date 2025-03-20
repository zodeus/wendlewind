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
        _pawnEffectsPanel = new PawnBodyEffectsPanel(playerPawn)
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(15)
        };
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities,
            lootContainer
        )
        {
            MaxHeight = 500,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visible = !playerPawn.IsDead, Width = 700
        };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);
        if (lootContainer != null)
        {
            _otherContainerPanel = new ItemContainerPanel(gui, lootContainer, playerPawn.Inventory.Entities)
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Loot], VerticalAlignment = VerticalAlignment.Stretch,
            };
        }


        VerticalStackPanel rightColumn = new()
        {
            Margin = new Thickness(20, 0, 0, 0),
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Large) { Text = "Ground Loot", Margin = new Thickness(0, 50, 0, 0), });
        if (_otherContainerPanel != null)
        {
            rightColumn.Widgets.Add(_otherContainerPanel);
        }

        HorizontalStackPanel grid = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                _pawnEffectsPanel,
                _bodyPanel,
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Fill },
                    Widgets =
                    {
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.Combat, item => gui.ViewEntity(item)),
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.NonCombat, item => gui.ViewEntity(item)),
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