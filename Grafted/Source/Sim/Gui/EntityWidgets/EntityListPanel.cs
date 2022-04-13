using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.UI;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets;

public class EntityListPanelItem : HorizontalStackPanel {
    private readonly Entity _entity;
    private readonly Label _label;

    public EntityListPanelItem(Entity entity, Action<Entity>? leftClickAction = null, Action<Entity>? rightClickAction = null) {
        Spacing = 10;
        _entity = entity;
        _label = new Label { VerticalAlignment = VerticalAlignment.Center, Font = BaseContent.Fonts.Default.Normal };
        ImageButton viewEntityButton = new() { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.QuestionMark], Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center };
        viewEntityButton.TouchDown += (_, _) => {
            Core.Sim.Gui!.ViewEntity(entity);
        };
        AddChild(viewEntityButton);
        HorizontalStackPanel entityButton = new() {
            Widgets = {
                new Image { Background = new TextureRegion(entity.Icon), Width = 32, Height = 32 },
                _label
            }
        };
        AddChild(entityButton);
        entityButton.TouchDown += (_, _) => {
            if (Input.LeftMouseButtonPressed) {
                leftClickAction?.Invoke(_entity);
            }

            if (Input.RightMouseButtonPressed) {
                rightClickAction?.Invoke(_entity);
            }
        };
    }

    public void Update() {
        _label.Text = _entity.Label;
    }
}

public class EntityListPanel : VerticalStackPanel {
    private readonly IEntityContainer _container;
    private readonly Action<Entity>? _rightClickAction;
    private readonly Action<Entity>? _leftClickAction;
    private readonly Dictionary<Entity, EntityListPanelItem> _items = new();

    private Func<Entity, bool>? _filter { get; }

    public EntityListPanel(IEntityContainer container, Func<Entity, bool>? filter = null, Action<Entity>? leftClickAction = null, Action<Entity>? rightClickAction = null) {
        Spacing = 5;
        _container = container;
        _leftClickAction = leftClickAction;
        _rightClickAction = rightClickAction;
        _filter = filter;
    }

    public void Update() {

        foreach (Entity entity in _container) {
            if (_filter != null && _filter(entity) == false) {
                continue;
            }

            if (!_items.ContainsKey(entity)) {
                _items[entity] = new EntityListPanelItem(entity, _leftClickAction, _rightClickAction);
                AddChild(_items[entity]);
            }
        }

        foreach ((Entity item, EntityListPanelItem panel) in _items) {
            if (item.IsDestroyed || _container.Contains(item) == false) {
                panel.RemoveFromParent();
                _items.Remove(item);
                continue;
            }

            panel.Update();
        }
    }
}