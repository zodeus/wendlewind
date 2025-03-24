using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using Grafted.Sim.Entities.Items.Trinkets;

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
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn)
        {
            Width = 500
        };
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities, null
        ) { MinHeight = 700, MaxHeight = 1000, Width = 600, VerticalAlignment = VerticalAlignment.Stretch };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);

        VerticalStackPanel rightColumn = new() { Spacing = 30 };
        rightColumn.Widgets.Add(_equipmentPanel);
        rightColumn.Widgets.Add(_pawnEffectsPanel);
        rightColumn.Widgets.Add(new PawnSkillsPanel(playerPawn.Skills));

        HorizontalStackPanel grid = new()
        {
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                _bodyPanel,
                new VerticalStackPanel
                {
                    Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Fill },
                    Spacing = 5,
                    Widgets =
                    {
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.Combat, item => gui.ViewEntity(item), false),
                        new TrinketBar(playerPawn.Inventory.Entities, TrinketType.NonCombat, item => gui.ViewEntity(item), false),
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
        _pawnEffectsPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }
}