using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class InventoryListPanel : VerticalStackPanel, IUpdatable
{
    private const int Columns = 3;
    private const int TileHeight = 72;

    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Func<Item, bool> _filter;
    private readonly Grid _grid;
    private readonly Label _empty;
    private readonly Dictionary<Item, InventoryItemTile> _tiles = [];
    private string _signature = "";

    public InventoryListPanel(BaseGui gui, PawnInventory inventory, Func<Item, bool> filter)
    {
        _gui = gui;
        _inventory = inventory;
        _filter = filter;
        Spacing = 8;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _grid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        for (var i = 0; i < Columns; i++)
        {
            _grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        }

        _empty = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "None in inventory",
            TextColor = new Color(140, 140, 140),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 24, 0, 0)
        };

        Widgets.Add(_grid);
        Widgets.Add(_empty);
        Rebuild();
    }

    public void Update()
    {
        var items = CurrentItems();
        var signature = ItemSignature(items);
        if (signature != _signature)
        {
            Rebuild();
            return;
        }

        foreach (var tile in _tiles.Values)
        {
            tile.Refresh();
        }
    }

    private List<Item> CurrentItems()
    {
        return _inventory
            .Where(item => !item.IsDestroyed && item.StackSize > 0 && _filter(item))
            .OrderBy(item => item.Label)
            .ToList();
    }

    private static string ItemSignature(List<Item> items)
    {
        return string.Join(",", items.Select(item => item.Id));
    }

    private void Rebuild()
    {
        var items = CurrentItems();
        _signature = ItemSignature(items);
        _tiles.Clear();
        _grid.Widgets.Clear();
        _grid.RowsProportions.Clear();

        _empty.Visible = items.Count == 0;
        _grid.Visible = items.Count > 0;
        if (items.Count == 0)
        {
            return;
        }

        var rowCount = (items.Count + Columns - 1) / Columns;
        for (var i = 0; i < rowCount; i++)
        {
            _grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var tile = new InventoryItemTile(_gui, item)
            {
                Height = TileHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _tiles[item] = tile;
            _grid.Widgets.Add(tile);
            Grid.SetColumn(tile, i % Columns);
            Grid.SetRow(tile, i / Columns);
        }
    }
}

internal sealed class InventoryItemTile : CursorButton
{
    private readonly Item _item;
    private readonly Label _stackLabel;

    public InventoryItemTile(BaseGui gui, Item item) : base(BaseContent.Styles.Button.Dark)
    {
        _item = item;
        Padding = new Thickness(8, 6, 10, 6);

        var icon = new Panel
        {
            Width = BaseContent.IconSizes.Large + 8,
            Height = BaseContent.IconSizes.Large + 8,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Image
                {
                    Background = item.GetIconImage(),
                    Width = BaseContent.IconSizes.Large,
                    Height = BaseContent.IconSizes.Large,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        var name = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Label,
            VerticalAlignment = VerticalAlignment.Center,
            Wrap = true,
            MaxWidth = 220
        };

        _stackLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };

        var text = new VerticalStackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { name, _stackLabel }
        };

        Content = new HorizontalStackPanel
        {
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { icon, text }
        };

        Click += (_, _) => gui.ViewEntity(item);
        this.WithDynamicTooltip(() => item.Label, () => item.Description);
        Refresh();
    }

    public void Refresh()
    {
        _stackLabel.Text = _item.IsStackable ? $"x{_item.StackSize}" : "";
        _stackLabel.Visible = _item.IsStackable;
    }
}
