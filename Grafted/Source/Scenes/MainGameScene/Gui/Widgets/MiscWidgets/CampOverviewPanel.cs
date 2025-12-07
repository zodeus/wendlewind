using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Grafted.Sim.Entities.Items.Trinkets;
using Microsoft.Xna.Framework;  
using Myra.Graphics2D.Brushes;
namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class CampOverviewPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;
    private readonly PawnBodyEffectsPanel _pawnEffectsPanel;

    public CampOverviewPanel(BaseGui gui, GameContext context)
    {
        var playerPawn = context.PlayerPawn;
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn) {
            Margin = new Thickness(0, 20, 0, 20)
        };
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory, null
        ) { 
            //Border = new SolidBrush(Color.White),
            //BorderThickness = new Thickness(1),
            MinHeight = 700, MaxHeight = 700, Width = 600, VerticalAlignment = VerticalAlignment.Stretch 
        };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn)
        {
            Margin = new Thickness(0, 0, 0, 20)
        };

        VerticalStackPanel rightColumn = new() { Spacing = 0 };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(new TrinketBar(playerPawn.Inventory, TrinketType.Combat, item => gui.ViewEntity(item)));
        rightColumn.Widgets.Add(new TrinketBar(playerPawn.Inventory, TrinketType.Passive, item => gui.ViewEntity(item)));
        rightColumn.Widgets.Add(new TrinketBar(playerPawn.Inventory, TrinketType.Interactive, item => gui.ViewEntity(item)));
        rightColumn.Widgets.Add(_pawnEffectsPanel);
        rightColumn.Widgets.Add(new PawnSkillsPanel(playerPawn.Skills));

        HorizontalStackPanel grid = new()
        {
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                _bodyPanel,
                _inventoryPanel,
                rightColumn
            }
        };
        Widgets.Add(grid);
    }

    public void Update()
    {
        _bodyPanel.Update();
        _pawnEffectsPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }
}