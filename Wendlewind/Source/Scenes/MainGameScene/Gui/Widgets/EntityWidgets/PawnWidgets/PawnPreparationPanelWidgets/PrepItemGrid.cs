using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class PrepItemGrid : VerticalStackPanel, IUpdatable
{
    private const int CellSize = 52;
    private const int CellSpacing = 4;

    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Func<Item, bool> _filter;
    private readonly Action<Item> _onClick;
    private readonly Func<Item, string> _tooltip;
    private readonly Func<Item, bool>? _isHighlighted;
    private readonly Func<Item, bool>? _isDisabled;
    private readonly Dictionary<Item, PrepItemButton> _buttons = [];
    private readonly VerticalStackPanel _rows = new() { Spacing = CellSpacing };
    private readonly Label _empty;
    private int _lastCount = -1;
    private int _iconsPerRow = -1;

    public PrepItemGrid(
        BaseGui gui,
        PawnInventory inventory,
        Func<Item, bool> filter,
        Action<Item> onClick,
        Func<Item, string> tooltip,
        Func<Item, bool>? isHighlighted = null,
        Func<Item, bool>? isDisabled = null)
    {
        _gui = gui;
        _inventory = inventory;
        _filter = filter;
        _onClick = onClick;
        _tooltip = tooltip;
        _isHighlighted = isHighlighted;
        _isDisabled = isDisabled;
        Spacing = 4;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _empty = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "None in inventory",
            TextColor = new Color(140, 140, 140)
        };

        Widgets.Add(_rows);
        Widgets.Add(_empty);
        Rebuild();
    }

    public void Update()
    {
        var items = CurrentItems();
        var perRow = IconsPerRow();
        if (perRow != _iconsPerRow
            || items.Count != _lastCount
            || items.Any(i => !_buttons.ContainsKey(i))
            || _buttons.Keys.Any(i => !items.Contains(i)))
        {
            Rebuild();
            return;
        }

        foreach (var button in _buttons.Values)
        {
            button.Refresh();
        }
    }

    private int IconsPerRow()
    {
        var width = Math.Max(ActualBounds.Width, Bounds.Width);
        if (width <= 0)
        {
            return 1;
        }

        return Math.Max(1, (width + CellSpacing) / (CellSize + CellSpacing));
    }

    private List<Item> CurrentItems()
    {
        return _inventory
            .Where(i => !i.IsDestroyed && i.StackSize > 0 && _filter(i))
            .OrderBy(i => i.Label)
            .ToList();
    }

    private void Rebuild()
    {
        var items = CurrentItems();
        _lastCount = items.Count;
        _iconsPerRow = IconsPerRow();
        _buttons.Clear();
        _rows.Widgets.Clear();
        _empty.Visible = items.Count == 0;

        HorizontalStackPanel? row = null;
        for (var i = 0; i < items.Count; i++)
        {
            if (i % _iconsPerRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = 4 };
                _rows.Widgets.Add(row);
            }

            var item = items[i];
            var button = new PrepItemButton(_gui, item, _onClick, _tooltip, _isHighlighted, _isDisabled);
            _buttons[item] = button;
            row!.Widgets.Add(button);
        }
    }
}

internal sealed class PrepItemButton : CursorButton
{
    private readonly Item _item;
    private readonly Label _stackLabel;
    private readonly Image _icon;
    private readonly Func<Item, string> _tooltip;
    private readonly Func<Item, bool>? _isHighlighted;
    private readonly Func<Item, bool>? _isDisabled;
    private readonly ColoredRegion _tint;

    public PrepItemButton(
        BaseGui gui,
        Item item,
        Action<Item> onClick,
        Func<Item, string> tooltip,
        Func<Item, bool>? isHighlighted,
        Func<Item, bool>? isDisabled) : base(BaseContent.Styles.Button.Icon)
    {
        _item = item;
        _tooltip = tooltip;
        _isHighlighted = isHighlighted;
        _isDisabled = isDisabled;

        var container = new Panel();
        _tint = new ColoredRegion(new TextureRegion(item.GetIcon()), Color.White);
        _icon = new Image
        {
            Background = _tint,
            Width = BaseContent.IconSizes.Medium,
            Height = BaseContent.IconSizes.Medium
        };
        container.Widgets.Add(_icon);

        _stackLabel = new Label(BaseContent.Styles.Label.Small)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            TextColor = Color.White
        };
        container.Widgets.Add(_stackLabel);

        Content = container;
        Width = BaseContent.IconSizes.Medium + 8;
        Height = BaseContent.IconSizes.Medium + 8;

        Click += (_, _) =>
        {
            if (_isDisabled?.Invoke(_item) == true)
            {
                return;
            }

            onClick(_item);
        };
        TouchDown += (_, _) =>
        {
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                gui.ViewEntity(_item);
            }
        };

        this.WithDynamicTooltip(() => _item.Label, () => _tooltip(_item));
        Refresh();
    }

    public void Refresh()
    {
        _stackLabel.Text = _item.StackSize > 1 ? _item.StackSize.ToString() : "";
        var disabled = _isDisabled?.Invoke(_item) == true;
        var highlighted = _isHighlighted?.Invoke(_item) == true;
        _tint.Color = disabled
            ? new Color(90, 90, 90)
            : highlighted
                ? new Color(255, 220, 140)
                : Color.White;
        Opacity = disabled ? 0.55f : 1f;
    }
}
