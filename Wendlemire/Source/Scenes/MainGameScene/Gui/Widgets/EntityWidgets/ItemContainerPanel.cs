namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class ItemContainerPanel : VerticalStackPanel, IUpdatable
{
    private static readonly (string Label, ItemType Type)[] Categories =
    [
        ("Equipment", ItemType.Equipment),
        ("Trinkets", ItemType.Trinket),
        ("Medicinal", ItemType.Medical),
        ("Potions", ItemType.Potion),
        ("Food", ItemType.Food),
        ("Incense", ItemType.Incense),
        ("Supplies", ItemType.Supplies),
        ("Enchantments", ItemType.Enchantment),
        ("Resources", ItemType.Resource)
    ];

    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly VerticalStackPanel _body;
    private readonly Label _empty;
    private readonly List<InventoryCardGrid> _grids = [];
    private string _signature = "";

    public ItemContainerPanel(BaseGui gui, PawnInventory inventory)
    {
        _gui = gui;
        _inventory = inventory;
        Padding = new Thickness(10);
        Width = 960;
        Height = 680;
        MinWidth = 960;

        _body = new VerticalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _empty = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Inventory is empty",
            TextColor = new Color(140, 140, 140),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 24, 0, 0)
        };

        var scroll = new TooltipAwareScrollViewer
        {
            Content = _body,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Widgets.Add(scroll);
        SetProportionType(scroll, ProportionType.Fill);
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

        foreach (var grid in _grids)
        {
            grid.Update();
        }
    }

    private List<Item> CurrentItems()
    {
        return _inventory
            .Where(item => !item.IsDestroyed && item.StackSize > 0)
            .ToList();
    }

    private static string ItemSignature(List<Item> items)
    {
        return string.Join(",", items.Select(item => $"{item.Id}:{item.ItemDef.ItemType}"));
    }

    private void Rebuild()
    {
        var items = CurrentItems();
        _signature = ItemSignature(items);
        _grids.Clear();
        _body.Widgets.Clear();

        if (items.Count == 0)
        {
            _body.Widgets.Add(_empty);
            return;
        }

        var grouped = items
            .GroupBy(item => item.ItemDef.ItemType)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Label).ToList());

        foreach (var (label, type) in Categories)
        {
            if (!grouped.TryGetValue(type, out var groupItems))
            {
                continue;
            }

            AddGroup(label, groupItems);
        }

        foreach (var leftover in grouped.Keys.Except(Categories.Select(category => category.Type)))
        {
            AddGroup("Other", grouped[leftover]);
        }
    }

    private void AddGroup(string label, List<Item> items)
    {
        var grid = new InventoryCardGrid(_gui, items);
        _grids.Add(grid);
        _body.Widgets.Add(new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"{label} · {items.Count}",
                    TextColor = Color.Goldenrod
                },
                grid
            }
        });
    }
}
