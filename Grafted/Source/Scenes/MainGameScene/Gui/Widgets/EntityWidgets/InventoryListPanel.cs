namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityListPanelItem : HorizontalStackPanel
{
    private readonly Entity _entity;
    private readonly Label _label;

    public EntityListPanelItem(BaseGui gui, Entity entity)
    {
        Spacing = 10;
        _entity = entity;
        _label = new Label { VerticalAlignment = VerticalAlignment.Center, Font = BaseContent.Fonts.Default.Normal };
        HorizontalStackPanel entityButton = new()
        {
            Spacing = 10,
            Widgets =
            {
                new Image { Background = new TextureRegion(entity.Icon), Width = BaseContent.IconSizes.Default, Height = BaseContent.IconSizes.Default },
                _label
            }
        };
        Widgets.Add(entityButton);
        entityButton.TouchDown += (_, _) =>
        {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                gui.ViewEntity(entity);
            }
        };
    }

    public void Update()
    {
        _label.Text = _entity is Item item ? item.LabelWithStackSize : _entity.Label;
    }
}

public class InventoryListPanel : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnInventory _inventory;
    private readonly Dictionary<Item, EntityListPanelItem> _itemPanels = new();

    private Func<Entity, bool>? _filter { get; }

    public InventoryListPanel(BaseGui gui, string label, PawnInventory inventory, Func<Entity, bool>? filter = null)
    {
        _gui = gui;
        _inventory = inventory;
        _filter = filter;
        var itemVerticalPanel = new VerticalStackPanel { Spacing = 5};
        
        //Widgets.Add(new HorizontalSeparator());
        Widgets.Add(new Label( /*BaseContent.Styles.Label.Medium*/) { Text = label, TextColor = Color.DarkGoldenrod });
        Widgets.Add(new ScrollViewer { Content = itemVerticalPanel, MaxHeight = 240 });
    }

    public void Update()
    {
        foreach (var item in _inventory)
        {
            if (_filter != null && _filter(item) == false)
            {
                continue;
            }

            if (!_itemPanels.ContainsKey(item))
            {
                _itemPanels[item] = new EntityListPanelItem(_gui, item)
                {
                    Margin = new Thickness(0, 0, 0,  5)
                };
                Widgets.Add(_itemPanels[item]);
            }
        }

        foreach ((var item, var panel) in _itemPanels)
        {
            if (item.IsDestroyed || _inventory.Contains(item) == false)
            {
                panel.RemoveFromParent();
                _itemPanels.Remove(item);
                continue;
            }

            panel.Update();
        }

        Visible = _inventory.Any(item => _filter == null || _filter(item));
    }
}