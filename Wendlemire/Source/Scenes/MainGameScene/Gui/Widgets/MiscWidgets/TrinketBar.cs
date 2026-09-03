namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class TrinketBar : VerticalStackPanel, IUpdatable
{
    private static readonly int CellSize = BaseContent.IconSizes.Large;
    private const int CellSpacing = 2;

    private readonly PawnInventory _inventory;
    private readonly TrinketType _type;
    private readonly Action<Item>? _clickAction;
    private readonly Dictionary<Item, TrinketBarCell> _trinkets = [];
    private int _perRow = -1;

    public TrinketBar(PawnInventory inventory, TrinketType type, Action<Item>? clickAction = null)
    {
        _inventory = inventory;
        _type = type;
        _clickAction = clickAction;
        Spacing = CellSpacing;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        inventory.ItemAdded += _ => Rebuild();
        Rebuild();
    }

    public void Update()
    {
        var perRow = CellsPerRow();
        if (perRow != _perRow)
        {
            Rebuild();
            return;
        }

        foreach (var cell in _trinkets.Values)
        {
            cell.Update();
        }
    }

    private int CellsPerRow()
    {
        var width = Math.Max(ActualBounds.Width, Bounds.Width);
        if (width <= 0)
        {
            return int.MaxValue;
        }

        return Math.Max(1, (width + CellSpacing) / (CellSize + CellSpacing));
    }

    private void Rebuild()
    {
        _perRow = CellsPerRow();
        _trinkets.Clear();
        Widgets.Clear();

        HorizontalStackPanel? row = null;
        var index = 0;
        foreach (var trinket in CurrentTrinkets())
        {
            if (index % _perRow == 0)
            {
                row = new HorizontalStackPanel { Spacing = CellSpacing };
                Widgets.Add(row);
            }

            var cell = new TrinketBarCell(trinket, _clickAction)
            {
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _trinkets[trinket] = cell;
            row!.Widgets.Add(cell);
            index++;
        }
    }

    private IEnumerable<Item> CurrentTrinkets()
    {
        foreach (var trinket in _inventory.Trinkets)
        {
            if (trinket.ItemDef.TrinketProperties?.Type == _type)
            {
                yield return trinket;
            }
        }
    }
}

public sealed class TrinketBarCell : CursorButton
{
    private static int Size => BaseContent.IconSizes.Large;
    private static int IconSize => Size - 12;

    private readonly Item _trinket;

    public TrinketBarCell(Item trinket, Action<Item>? clickAction)
    {
        _trinket = trinket;
        Width = Size;
        Height = Size;
        MinWidth = Size;
        MinHeight = Size;
        MaxWidth = Size;
        MaxHeight = Size;
        Padding = new Thickness(6);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Content = new Image
        {
            Background = trinket.GetIconImage(),
            Width = IconSize,
            Height = IconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Click += (_, _) =>
        {
            if (clickAction != null)
            {
                clickAction(trinket);
                return;
            }

            trinket.TrinketHandler?.OnClick();
        };

        this.WithDynamicTooltip(() => _trinket.Label, () => _trinket.Def.Description);
    }

    public void Update()
    {
        var handler = _trinket.TrinketHandler;
        if (handler == null)
        {
            return;
        }

        if (handler.IsActive)
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        }
        else if (handler.Cooldown > 0)
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameRed];
        }
        else
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        }

        TooltipHelper.UpdatePosition();
    }
}
