using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnPreparationPanel : Panel, IUpdatable
{
    private const int CharacterColumnWidth = 236;
    private const int CardPadding = 16;
    private const int PotionColumnWidth = 460;

    private readonly PawnEquipmentPanel _equipmentPanel;
    private readonly PawnSummaryCard _summaryCard;
    private readonly PawnBodyEffectsPanel _pawnEffectsPanel;
    private readonly MealPlanPanel _mealPlanPanel;
    private readonly IncenseChargesPanel _incenseChargesPanel;
    private readonly PotionsPanel _potionsPanel;
    private readonly MedicalChestPanel _medicalChestPanel;
    private readonly TrinketsPanel _trinketsPanel;
    private readonly Panel _controlsPanel;

    public PawnPreparationPanel(BaseGui gui, Pawn playerPawn)
    {
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(8);
        _pawnEffectsPanel = new PawnBodyEffectsPanel(gui, playerPawn);
        _summaryCard = new PawnSummaryCard(gui, playerPawn);

        _equipmentPanel = new PawnEquipmentPanel(gui, playerPawn);
        _controlsPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        _potionsPanel = new PotionsPanel(gui, playerPawn);
        _medicalChestPanel = new MedicalChestPanel(gui, playerPawn);
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
        root.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, CharacterColumnWidth));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, _equipmentPanel.PixelWidth + CardPadding));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, PotionColumnWidth));
        root.RowsProportions.Add(new Proportion(ProportionType.Part, 1.15f));
        root.RowsProportions.Add(new Proportion(ProportionType.Part, 0.85f));

        Place(root, _summaryCard, 0, 0);
        Grid.SetRowSpan(_summaryCard, 2);
        var equipment = CreateEquipmentCard();
        Place(root, equipment, 1, 0);
        Place(root, _trinketsPanel, 1, 1);
        Place(root, _medicalChestPanel, 2, 0);
        Grid.SetRowSpan(_medicalChestPanel, 2);

        var lastColumn = new Grid
        {
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        lastColumn.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        lastColumn.RowsProportions.Add(new Proportion(ProportionType.Auto));
        lastColumn.RowsProportions.Add(new Proportion(ProportionType.Part, 1f));
        lastColumn.RowsProportions.Add(new Proportion(ProportionType.Part, 1f));
        Place(lastColumn, _potionsPanel, 0, 0);
        Place(lastColumn, _mealPlanPanel, 0, 1);
        Place(lastColumn, _incenseChargesPanel, 0, 2);
        Place(root, lastColumn, 3, 0);
        Grid.SetRowSpan(lastColumn, 2);

        var grimoireButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Grimoire" },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        grimoireButton.Click += (_, _) => gui.OpenGrimoire();

        var inventoryButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Inventory" },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        inventoryButton.Click += (_, _) => gui.OpenInventory();

        var header = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                grimoireButton,
                inventoryButton,
                _controlsPanel
            }
        };

        var layout = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Widgets =
            {
                header,
                root
            }
        };
        VerticalStackPanel.SetProportionType(root, ProportionType.Fill);
        Widgets.Add(layout);
    }

    private Widget CreateEquipmentCard()
    {
        var body = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left,
            Widgets =
            {
                _equipmentPanel,
                _pawnEffectsPanel
            }
        };

        return new PrepCard("Equipment", body);
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
        _mealPlanPanel.Update();
        _incenseChargesPanel.Update();
        _medicalChestPanel.Update();
        _potionsPanel.Update();
        _potionsPanel.SyncCardHeight(_medicalChestPanel.CardHeight);
        _trinketsPanel.Update();
    }
}
