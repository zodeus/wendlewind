namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntityListPanelItem : HorizontalStackPanel
{
    private readonly Entity _entity;
    private readonly Label _label;

    public EntityListPanelItem(BaseGui gui, Entity entity, Action<Entity>? leftClickAction = null, Action<Entity>? rightClickAction = null)
    {
        Spacing = 10;
        _entity = entity;
        _label = new Label { VerticalAlignment = VerticalAlignment.Center, Font = BaseContent.Fonts.Default.Normal };
        var viewEntityButton = new Button()
            { Content = new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.QuestionMark], Width = BaseContent.IconSizes.Small, Height = BaseContent.IconSizes.Small }, VerticalAlignment = VerticalAlignment.Center };
        viewEntityButton.TouchDown += (_, _) => { gui.ViewEntity(entity); };
        Widgets.Add(viewEntityButton);
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
            Log.Debug($"TouchDown on {_entity.Label}, LeftDown={Mouse.GetState().LeftButton == ButtonState.Pressed}, RightDown={Mouse.GetState().RightButton == ButtonState.Pressed}");
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                leftClickAction?.Invoke(_entity);
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                rightClickAction?.Invoke(_entity);
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
    private readonly Action<Entity>? _rightClickAction;
    private readonly Action<Entity>? _leftClickAction;
    private readonly Dictionary<Item, EntityListPanelItem> _itemPanels = new();

    private Func<Entity, bool>? _filter { get; }

    public InventoryListPanel(BaseGui gui, string label, PawnInventory inventory, Func<Entity, bool>? filter = null, Action<Entity>? leftClickAction = null, Action<Entity>? rightClickAction = null)
    {
        _gui = gui;
        _inventory = inventory;
        _leftClickAction = leftClickAction;
        _rightClickAction = rightClickAction;
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
                _itemPanels[item] = new EntityListPanelItem(_gui, item, _leftClickAction, _rightClickAction)
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