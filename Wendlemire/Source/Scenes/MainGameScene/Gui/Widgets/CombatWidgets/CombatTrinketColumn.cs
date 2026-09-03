namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatTrinketColumn : ScrollViewer, IUpdatable
{
    public const int ScrollBarWidth = 20;
    public static int ColumnWidth => BaseContent.IconSizes.Large + ScrollBarWidth;

    private readonly PawnInventory _inventory;
    private readonly Action<Item> _clickAction;
    private readonly VerticalStackPanel _list;
    private readonly Dictionary<Item, TrinketBarCell> _cells = [];
    private int _lastWheelValue = Mouse.GetState().ScrollWheelValue;

    public CombatTrinketColumn(BaseGui gui, Pawn pawn)
    {
        _inventory = pawn.Inventory;
        _clickAction = item => gui.ViewEntity(item);
        _list = new VerticalStackPanel
        {
            Spacing = 2,
            Width = BaseContent.IconSizes.Large,
            MinWidth = BaseContent.IconSizes.Large,
            MaxWidth = BaseContent.IconSizes.Large,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        Content = _list;
        ShowHorizontalScrollBar = false;
        ShowVerticalScrollBar = true;
        Width = ColumnWidth;
        MinWidth = ColumnWidth;
        MaxWidth = ColumnWidth;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;
        _inventory.ItemAdded += _ => Rebuild();
        _inventory.ItemRemoved += _ => Rebuild();
        Rebuild();
    }

    public override Widget? HitTest(Point p)
    {
        if (!Visible)
        {
            return null;
        }

        if (ContainsGlobalPoint(p))
        {
            return base.HitTest(p) ?? this;
        }

        return null;
    }

    public override void OnMouseWheel(float delta)
    {
        if (TooltipHelper.CapturesMouseWheel)
        {
            return;
        }

        base.OnMouseWheel(delta);
    }

    public void Update()
    {
        foreach (var cell in _cells.Values)
        {
            cell.Update();
        }

        var overColumn = Desktop != null && ContainsGlobalPoint(Desktop.MousePosition);
        ApplyPolledWheel(overColumn);
    }

    private void ApplyPolledWheel(bool overColumn)
    {
        var wheel = Mouse.GetState().ScrollWheelValue;
        var delta = wheel - _lastWheelValue;
        _lastWheelValue = wheel;
        if (!overColumn || delta == 0 || TooltipHelper.CapturesMouseWheel)
        {
            return;
        }

        var pos = ScrollPosition;
        pos.Y = Math.Clamp(pos.Y - delta, 0, Math.Max(0, ScrollMaximum.Y));
        ScrollPosition = pos;
    }

    private void Rebuild()
    {
        _cells.Clear();
        _list.Widgets.Clear();

        foreach (var trinket in CurrentTrinkets())
        {
            var cell = new TrinketBarCell(trinket, _clickAction);
            _cells[trinket] = cell;
            _list.Widgets.Add(cell);
        }

        Visible = _cells.Count > 0;
    }

    private IEnumerable<Item> CurrentTrinkets()
    {
        foreach (var trinket in _inventory.Trinkets)
        {
            var type = trinket.ItemDef.TrinketProperties?.Type ?? TrinketType.Invalid;
            if (type is TrinketType.Combat or TrinketType.Passive or TrinketType.Interactive)
            {
                yield return trinket;
            }
        }
    }
}
