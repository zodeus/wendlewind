using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnPreparationPanel : Panel, IUpdatable
{
    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnSummaryCard _summaryCard;
    private readonly PawnBodyEffectsPanel _pawnEffectsPanel;
    private readonly SupplyItemsBar _supplyItemsBar;
    private readonly MealPlanPanel _mealPlanPanel;
    private readonly IncenseChargesPanel _incenseChargesPanel;
    private readonly PotionsPanel _potionsPanel;
    private readonly MedicalChestPanel _medicalChestPanel;
    private readonly TrinketsPanel _trinketsPanel;
    private readonly WeaponBar _weaponBar;
    private Panel _controlsPanel;

    public PawnPreparationPanel(BaseGui gui, Pawn playerPawn)
    {
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(8);
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn);
        _summaryCard = new PawnSummaryCard(gui, playerPawn);

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);
        _controlsPanel = new Panel { HorizontalAlignment = HorizontalAlignment.Left };
        _weaponBar = new WeaponBar(playerPawn);
        _potionsPanel = new PotionsPanel(gui, playerPawn);
        _medicalChestPanel = new MedicalChestPanel(gui, playerPawn);
        _supplyItemsBar = new SupplyItemsBar(gui, playerPawn.Inventory);
        _mealPlanPanel = new MealPlanPanel(gui, playerPawn);
        _incenseChargesPanel = new IncenseChargesPanel(gui, playerPawn);
        _trinketsPanel = new TrinketsPanel(gui, playerPawn);

        var root = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        root.ColumnsProportions.Add(new Proportion(ProportionType.Part, 0.9f));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1.15f));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1f));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1f));
        root.RowsProportions.Add(new Proportion(ProportionType.Part, 1.15f));
        root.RowsProportions.Add(new Proportion(ProportionType.Part, 0.85f));

        Place(root, _summaryCard, 0, 0);
        var loadout = CreateLoadoutCard(playerPawn);
        Place(root, loadout, 1, 0);
        Grid.SetRowSpan(loadout, 2);
        Place(root, _potionsPanel, 2, 0);
        Place(root, _medicalChestPanel, 3, 0);
        Place(root, CreateTrinketsCell(), 0, 1);
        Place(root, _mealPlanPanel, 2, 1);
        Place(root, _incenseChargesPanel, 3, 1);
        Widgets.Add(root);
    }

    private Widget CreateTrinketsCell()
    {
        var cell = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Widgets =
            {
                _trinketsPanel,
                _supplyItemsBar,
                _controlsPanel
            }
        };
        VerticalStackPanel.SetProportionType(_trinketsPanel, ProportionType.Fill);
        return cell;
    }

    private Widget CreateLoadoutCard(Pawn playerPawn)
    {
        var body = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left,
            Widgets =
            {
                new HorizontalStackPanel
                {
                    Spacing = 12,
                    Widgets =
                    {
                        _equipmentPanel,
                        new PawnSkillsPanel(playerPawn.Skills)
                    }
                },
                new Label(BaseContent.Styles.Label.Small) { Text = "Stance", TextColor = new Color(180, 180, 180) },
                new BodyStanceBar(playerPawn),
                new Label(BaseContent.Styles.Label.Small) { Text = "Weapons", TextColor = new Color(180, 180, 180) },
                _weaponBar,
                _pawnEffectsPanel
            }
        };

        return new PrepCard("Loadout", body);
    }

    private static void Place(Grid grid, Widget widget, int column, int row)
    {
        grid.Widgets.Add(widget);
        Grid.SetColumn(widget, column);
        Grid.SetRow(widget, row);
    }

    public void SetControls(Widget control)
    {
        _controlsPanel.Widgets.Add(control);
    }

    public void Update()
    {
        _summaryCard.Update();
        _equipmentPanel.Update();
        _pawnEffectsPanel.Update();
        _supplyItemsBar.Update();
        _mealPlanPanel.Update();
        _incenseChargesPanel.Update();
        _potionsPanel.Update();
        _medicalChestPanel.Update();
        _weaponBar.Update();
    }
}
