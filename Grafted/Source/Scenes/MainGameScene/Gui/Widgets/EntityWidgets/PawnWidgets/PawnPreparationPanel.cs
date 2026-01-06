using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnPreparationPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnBodyEffectsPanel _pawnEffectsPanel;

    public PawnPreparationPanel(BaseGui gui, Pawn playerPawn)
    {
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn);
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body)
        {
            Height = 700,
        };
        _inventoryPanel = new ItemContainerPanel(gui, playerPawn.Inventory)
        {
            Width = 400,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visible = !playerPawn.IsDead
        };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);

        VerticalStackPanel centerColumn = new()
        {
            Spacing = 15,
            Margin = new Thickness(20, 0, 0, 0)
        };
        
        centerColumn.Widgets.Add(new HorizontalStackPanel {
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

    public void Update()
    {
        _bodyPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
        _pawnEffectsPanel.Update();
    }
}