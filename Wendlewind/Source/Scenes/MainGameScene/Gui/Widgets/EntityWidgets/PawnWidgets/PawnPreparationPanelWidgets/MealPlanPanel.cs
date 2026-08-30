namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class MealPlanPanel : PrepCard, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly FillGauge _gauge;
    private readonly PrepItemGrid _inventory;

    public MealPlanPanel(BaseGui gui, Pawn pawn) : base("Food")
    {
        _pawn = pawn;

        Body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Eaten at the start of each battle",
            TextColor = new Color(160, 160, 160)
        });

        _gauge = new FillGauge();
        Body.Widgets.Add(_gauge);
        RefreshGauge();

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.FoodProperties != null,
            ToggleMeal,
            MealTooltip,
            item => _pawn.MealPlan.Items.Contains(item),
            item => !_pawn.MealPlan.Items.Contains(item) && !_pawn.MealPlan.CanFit(item));
        SetInventory(_inventory);
    }

    public void Update()
    {
        _pawn.MealPlan.Prune();
        RefreshGauge();
        _inventory.Update();
    }

    private void RefreshGauge()
    {
        var current = _pawn.MealPlan.Items.Count;
        _gauge.Set(current, MealPlan.MaxSlots, $"{current} / {MealPlan.MaxSlots}");
    }

    private string MealTooltip(Item item)
    {
        if (_pawn.MealPlan.CanFit(item))
        {
            return "Click to add to meal";
        }

        if (_pawn.MealPlan.Items.Contains(item))
        {
            return "In meal — click to remove";
        }

        return $"Meal full ({MealPlan.MaxSlots}/{MealPlan.MaxSlots})";
    }

    private void ToggleMeal(Item item)
    {
        if (_pawn.MealPlan.CanFit(item))
        {
            _pawn.MealPlan.TryAdd(item);
        }
        else if (_pawn.MealPlan.Items.Contains(item))
        {
            _pawn.MealPlan.Remove(item);
        }

        RefreshGauge();
    }
}
