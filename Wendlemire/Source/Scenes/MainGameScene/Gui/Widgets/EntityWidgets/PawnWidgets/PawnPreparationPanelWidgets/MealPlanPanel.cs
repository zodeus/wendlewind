using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class MealPlanPanel : PrepCard, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly VerticalStackPanel _slotRows;
    private readonly PrepBuffList _buffs;
    private readonly PrepItemGrid _inventory;
    private string _slotSignature = "";
    private int _slotsPerRow = -1;

    public MealPlanPanel(BaseGui gui, Pawn pawn) : base("Food")
    {
        _gui = gui;
        _pawn = pawn;

        Body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Eaten at the start of each battle",
            TextColor = new Color(160, 160, 160)
        });

        _slotRows = new VerticalStackPanel { Spacing = PrepSlots.Spacing };
        Body.Widgets.Add(_slotRows);
        _buffs = new PrepBuffList();
        Body.Widgets.Add(_buffs);

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.FoodProperties != null,
            ToggleMeal,
            MealTooltip,
            isDisabled: IsMuted,
            pagedRow: true);
        SetInventory(_inventory, 64);
        RebuildSlots();
    }

    public void Update()
    {
        _pawn.MealPlan.Prune();
        var perRow = SlotsPerRow();
        var signature = SlotSignature();
        if (perRow != _slotsPerRow || signature != _slotSignature)
        {
            RebuildSlots();
        }

        _inventory.Update();
    }

    private bool IsMuted(Item item)
    {
        return _pawn.MealPlan.Items.Contains(item) || !_pawn.MealPlan.CanFit(item);
    }

    private string MealTooltip(Item item)
    {
        if (_pawn.MealPlan.CanFit(item))
        {
            return "Click to add to a meal slot";
        }

        if (_pawn.MealPlan.Items.Contains(item))
        {
            return "In meal — click a slot below to remove";
        }

        if (_pawn.MealPlan.Items.Count >= _pawn.MealPlan.Capacity)
        {
            return "Unlock more food slots in later rounds";
        }

        return "No empty meal slots";
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

        RebuildSlots();
    }

    private void RebuildSlots()
    {
        _slotsPerRow = SlotsPerRow();
        _slotSignature = SlotSignature();
        _slotRows.Widgets.Clear();

        HorizontalStackPanel? row = null;
        for (var i = 0; i < MealPlan.MaxSlots; i++)
        {
            if (i % _slotsPerRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = PrepSlots.Spacing };
                _slotRows.Widgets.Add(row);
            }

            var index = i;
            row!.Widgets.Add(index < _pawn.MealPlan.Items.Count
                ? FilledSlot(_pawn.MealPlan.Items[index], index)
                : index < _pawn.MealPlan.Capacity
                    ? EmptySlot()
                    : LockedSlot(index + 1));
        }

        _buffs.SetEffects(PrepBuffList.FromMeal(_pawn));
    }

    private Widget FilledSlot(Item item, int index)
    {
        var icon = new CursorButton
        {
            Width = PrepSlots.Size - PrepSlots.Pad * 2,
            Height = PrepSlots.Size - PrepSlots.Pad * 2,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Image
            {
                Background = item.GetIconImage(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            }
        };
        icon.Click += (_, _) =>
        {
            _pawn.MealPlan.RemoveAt(index);
            RebuildSlots();
        };
        icon.TouchDown += (_, _) =>
        {
            if (Mouse.GetState().RightButton == ButtonState.Pressed && !item.IsDestroyed)
            {
                _gui.ViewEntity(item);
            }
        };
        icon.WithTooltip(item.Label, "Click to remove from meal");
        return PrepSlots.Frame(icon);
    }

    private static Widget EmptySlot()
    {
        var empty = new Panel();
        empty.WithTooltip("Empty meal slot");
        return PrepSlots.Frame(empty);
    }

    private static Widget LockedSlot(int slotNumber)
    {
        var tip = SlotUnlockTooltip.ForSlot(PrepSlotKind.Food, slotNumber);
        return LockedSlotChrome.Slot(tip.title, tip.description);
    }

    private int SlotsPerRow()
    {
        var width = Math.Max(_slotRows.ActualBounds.Width, _slotRows.Bounds.Width);
        if (width <= 0)
        {
            return Math.Max(1, MealPlan.MaxSlots);
        }

        return Math.Max(1, (width + PrepSlots.Spacing) / (PrepSlots.Size + PrepSlots.Spacing));
    }

    private string SlotSignature()
    {
        return _pawn.MealPlan.Capacity + ":" + string.Join(",", _pawn.MealPlan.Items.Select(i => i?.Id ?? -1));
    }
}
