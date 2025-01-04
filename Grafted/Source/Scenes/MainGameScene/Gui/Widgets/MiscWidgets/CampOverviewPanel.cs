using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class CampOverviewPanel : Panel, IUpdatable
{
    private readonly ItemContainerPanel _inventoryPanel;
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnBodyPanel _bodyPanel;

    public CampOverviewPanel(BaseGui gui, Pawn playerPawn)
    {
        _bodyPanel = new PawnBodyPanel(gui, playerPawn.Body);
        _inventoryPanel = new ItemContainerPanel(gui,
            playerPawn.Inventory.Entities, null
        ) { Visible = !playerPawn.IsDead, MinHeight = 700, Width = 700 };

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);

        VerticalStackPanel rightColumn = new()
        {
            Visible = !playerPawn.IsDead,
            Proportions = { Proportion.Auto, Proportion.Auto, Proportion.Auto, Proportion.Fill }
        };
        rightColumn.AddChild(_equipmentPanel);
        rightColumn.AddChild(new HorizontalSeparator { Margin = new Thickness(0, 50, 0, 20) });
        rightColumn.AddChild(new Label(BaseContent.Styles.Label.Large) { Text = "Augments" });
        rightColumn.AddChild(new VerticalStackPanel
        {
            Widgets =
            {
                new Label { Text = "  - Tarred Blood" },
                new Label { Text = "  - Synthetic Arteries" },
                new Label { Text = "  - Random Bits" },
                new Label { Text = "  - Toughened Arteries" },
                new Label { Text = "  - Blood Bloated" },
                new Label { Text = "  - Weaved Ligaments" },
                new Label { Text = "  - Elven Grace" },
                new Label { Text = "  - Regeneration Potion" },
            }
        });
        HorizontalStackPanel grid = new()
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                //ZonePanel(),
                _bodyPanel,
                _inventoryPanel,
                rightColumn
            }
        };
        AddChild(grid);
    }

    public void Update()
    {
        _bodyPanel.Update();
        _equipmentPanel.Update();
        _inventoryPanel.Update();
    }
}