using Grafted.Sim.Entities;

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
        ImageButton viewEntityButton = new()
            { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.QuestionMark], Width = BaseContent.IconSizes.Small, Height = BaseContent.IconSizes.Small, VerticalAlignment = VerticalAlignment.Center };
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
            if (Input.LeftMouseButtonPressed)
            {
                leftClickAction?.Invoke(_entity);
            }

            if (Input.RightMouseButtonPressed)
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

public class EntityListPanel : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly EntityContainer _container;
    private readonly Action<Entity>? _rightClickAction;
    private readonly Action<Entity>? _leftClickAction;
    private readonly Dictionary<Entity, EntityListPanelItem> _items = new();

    private Func<Entity, bool>? _filter { get; }

    public EntityListPanel(BaseGui gui, string label, EntityContainer container, Func<Entity, bool>? filter = null, Action<Entity>? leftClickAction = null, Action<Entity>? rightClickAction = null)
    {
        _gui = gui;
        _container = container;
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
        foreach (var entity in _container)
        {
            if (_filter != null && _filter(entity) == false)
            {
                continue;
            }

            if (!_items.ContainsKey(entity))
            {
                _items[entity] = new EntityListPanelItem(_gui, entity, _leftClickAction, _rightClickAction)
                {
                    Margin = new Thickness(0, 0, 0,  5)
                };
                Widgets.Add(_items[entity]);
            }
        }

        foreach ((var item, var panel) in _items)
        {
            if (item.IsDestroyed || _container.Contains(item) == false)
            {
                panel.RemoveFromParent();
                _items.Remove(item);
                continue;
            }

            panel.Update();
        }

        Visible = _items.Any();
    }
}