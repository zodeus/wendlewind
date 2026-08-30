using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal sealed class PrepItemGrid : VerticalStackPanel, IUpdatable
{
    public const int RowCells = 8;
    private const int CellSize = 52;
    private const int CellSpacing = 4;
    private static readonly int ButtonSize = BaseContent.IconSizes.Medium + 8;

    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Func<Item, bool> _filter;
    private readonly Action<Item> _onClick;
    private readonly Func<Item, string> _tooltip;
    private readonly Func<Item, bool>? _isHighlighted;
    private readonly Func<Item, bool>? _isDisabled;
    private readonly bool _pagedRow;
    private readonly Dictionary<Item, PrepItemButton> _buttons = [];
    private readonly VerticalStackPanel _rows = new() { Spacing = CellSpacing };
    private readonly Label _empty;
    private string _itemSignature = "";
    private int _iconsPerRow = -1;
    private int _page;

    public PrepItemGrid(
        BaseGui gui,
        PawnInventory inventory,
        Func<Item, bool> filter,
        Action<Item> onClick,
        Func<Item, string> tooltip,
        Func<Item, bool>? isHighlighted = null,
        Func<Item, bool>? isDisabled = null,
        bool pagedRow = false)
    {
        _gui = gui;
        _inventory = inventory;
        _filter = filter;
        _onClick = onClick;
        _tooltip = tooltip;
        _isHighlighted = isHighlighted;
        _isDisabled = isDisabled;
        _pagedRow = pagedRow;
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
        var signature = ItemSignature(items);
        if (perRow != _iconsPerRow || signature != _itemSignature)
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
            return _pagedRow ? RowCells : 1;
        }

        var fitted = Math.Max(1, (width + CellSpacing) / (CellSize + CellSpacing));
        return _pagedRow ? Math.Min(fitted, RowCells) : fitted;
    }

    private List<Item> CurrentItems()
    {
        return _inventory
            .Where(i => !i.IsDestroyed && i.StackSize > 0 && _filter(i))
            .OrderBy(i => i.Label)
            .ToList();
    }

    private static string ItemSignature(List<Item> items)
    {
        return string.Join(",", items.Select(i => i.Id));
    }

    private void Rebuild()
    {
        var items = CurrentItems();
        _itemSignature = ItemSignature(items);
        _iconsPerRow = IconsPerRow();
        _buttons.Clear();
        _rows.Widgets.Clear();

        if (_pagedRow)
        {
            RebuildPagedRow(items);
            return;
        }

        _empty.Visible = items.Count == 0;
        HorizontalStackPanel? row = null;
        for (var i = 0; i < items.Count; i++)
        {
            if (i % _iconsPerRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = CellSpacing };
                _rows.Widgets.Add(row);
            }

            var item = items[i];
            var button = new PrepItemButton(_gui, item, _onClick, _tooltip, _isHighlighted, _isDisabled);
            _buttons[item] = button;
            row!.Widgets.Add(button);
        }
    }

    private void RebuildPagedRow(List<Item> items)
    {
        _empty.Visible = false;
        var overflow = items.Count > _iconsPerRow;
        var visible = overflow ? _iconsPerRow - 1 : _iconsPerRow;
        var pageCount = Math.Max(1, (items.Count + visible - 1) / visible);
        if (_page >= pageCount)
        {
            _page = 0;
        }

        var row = new HorizontalStackPanel { Spacing = CellSpacing };
        _rows.Widgets.Add(row);
        var start = _page * visible;

        for (var i = 0; i < visible; i++)
        {
            var index = start + i;
            if (index < items.Count)
            {
                var item = items[index];
                var button = new PrepItemButton(_gui, item, _onClick, _tooltip, _isHighlighted, _isDisabled);
                _buttons[item] = button;
                row.Widgets.Add(button);
            }
            else
            {
                row.Widgets.Add(EmptyCell());
            }
        }

        if (overflow)
        {
            row.Widgets.Add(CreatePager(pageCount));
        }
    }

    private Widget CreatePager(int pageCount)
    {
        var lastPage = _page >= pageCount - 1;
        var icon = lastPage
            ? BaseContent.Styles.Atlas.Icon.ArrowPositive
            : BaseContent.Styles.Atlas.Icon.ArrowNegative;
        var button = new CursorButton(BaseContent.Styles.Button.Icon)
        {
            Width = ButtonSize,
            Height = ButtonSize,
            Content = new Image
            {
                Background = Stylesheet.Current.Atlas[icon],
                Width = BaseContent.IconSizes.Small,
                Height = BaseContent.IconSizes.Small,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        button.Click += (_, _) =>
        {
            _page = lastPage ? 0 : _page + 1;
            Rebuild();
        };
        button.WithTooltip(lastPage ? "Back to first row" : "Next row");
        return button;
    }

    private static Widget EmptyCell()
    {
        return new Panel
        {
            Width = ButtonSize,
            Height = ButtonSize,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        };
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
    private readonly ColoredIcon _tint;

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
        _tint = new ColoredIcon(item.GetIconImage(), Color.White);
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
