using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnPreparationPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnBodyEffectsPanel _pawnEffectsPanel;
    private readonly SupplyItemsBar _supplyItemsBar;
    private readonly FoodItemsBar _foodItemsBar;
    private readonly FlammableItemsBar _flammableItemsBar;
    private Panel _controlsPanel;

    public PawnPreparationPanel(BaseGui gui, Pawn playerPawn)
    {
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn);
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body, playerPawn.Inventory)
        {
            Height = 740,
        };
        _inventoryPanel = new ItemContainerPanel(gui, playerPawn.Inventory)
        {
            Width = 400,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visible = !playerPawn.IsDead
        };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);
        _controlsPanel = new Panel();

        VerticalStackPanel centerColumn = new()
        {
            Spacing = 15,
            Margin = new Thickness(20, 0, 0, 0)
        };

        centerColumn.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 15,
            Widgets = {
                _equipmentPanel,
                new PawnSkillsPanel(playerPawn.Skills)
            }
        });
        centerColumn.Widgets.Add(new TrinketBar(playerPawn.Inventory, TrinketType.Combat, item => gui.ViewEntity(item)) { TrinketsPerRow = 9 });
        centerColumn.Widgets.Add(new TrinketBar(playerPawn.Inventory, TrinketType.Passive, item => gui.ViewEntity(item)) { TrinketsPerRow = 9 });
        centerColumn.Widgets.Add(new TrinketBar(playerPawn.Inventory, TrinketType.Interactive, item => gui.ViewEntity(item)) { TrinketsPerRow = 9 });
        centerColumn.Widgets.Add(_pawnEffectsPanel);

        // Supply, food and flammable bars above zone controls
        _supplyItemsBar = new SupplyItemsBar(gui, playerPawn.Inventory);
        _foodItemsBar = new FoodItemsBar(gui, playerPawn);
        _flammableItemsBar = new FlammableItemsBar(gui, Core.Context.Player);
        var consumableBarsContainer = new HorizontalStackPanel { Spacing = 12 };
        consumableBarsContainer.Widgets.Add(_supplyItemsBar);
        consumableBarsContainer.Widgets.Add(_foodItemsBar);
        consumableBarsContainer.Widgets.Add(_flammableItemsBar);
        centerColumn.Widgets.Add(consumableBarsContainer);

        centerColumn.Widgets.Add(_controlsPanel);

        HorizontalStackPanel grid = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                _bodyPanel,
                centerColumn,
                _inventoryPanel,
            }
        };
        HorizontalStackPanel.SetProportionType(_bodyPanel, ProportionType.Auto);
        HorizontalStackPanel.SetProportionType(centerColumn, ProportionType.Fill);
        HorizontalStackPanel.SetProportionType(_inventoryPanel, ProportionType.Auto);
        Widgets.Add(grid);
    }

    public void SetControls(Widget control)
    {
        _controlsPanel.Widgets.Add(control);
    }
    
    public void Update()
    {
        _bodyPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
        _pawnEffectsPanel.Update();
        _supplyItemsBar.Update();
        _foodItemsBar.Update();
        _flammableItemsBar.Update();
    }
}