using System;
using System.Collections.Generic;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.UI;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets;

public class EntityListPanelItem : HorizontalStackPanel {
    private readonly Entity _entity;
    private readonly Label _label;
    private event Action<Entity>? RightClickAction;

    public EntityListPanelItem(Entity entity, Action<Entity>? rightClickAction = null) {
        RightClickAction = rightClickAction;
        Spacing = 10;
        _entity = entity;
        _label = new Label { VerticalAlignment = VerticalAlignment.Center, Font = BaseContent.Fonts.Default.Normal };
        AddChild(new Image { Background = new TextureRegion(entity.Icon), Width = 32, Height = 32 });
        AddChild(_label);
        TouchDown += (_, _) => {
            if (Input.LeftMouseButtonPressed) {
                Core.Sim.Gui!.ViewEntity(entity);
            }

            if (Input.RightMouseButtonPressed) {
                RightClickAction?.Invoke(_entity);
            }
        };
    }

    public void Update() {
        _label.Text = _entity is Item item ? item.LabelWithStackSize : _entity.Label;
    }
}

public class EntityListPanel : VerticalStackPanel {
    private readonly IEntityContainer _container;
    private readonly Action<Entity>? _rightClickAction;
    private readonly Dictionary<Entity, EntityListPanelItem> _items = new();

    private Func<Entity, bool>? _filter { get; }

    public EntityListPanel(IEntityContainer container, Func<Entity, bool>? filter = null, Action<Entity>? rightClickAction = null) {
        Spacing = 5;
        _container = container;
        _rightClickAction = rightClickAction;
        _filter = filter;
    }

    public void Update() {

        foreach (Entity entity in _container) {
            if (_filter != null && _filter(entity) == false) {
                continue;
            }

            if (!_items.ContainsKey(entity)) {
                _items[entity] = new EntityListPanelItem(entity, _rightClickAction);
                AddChild(_items[entity]);
            }
        }

        foreach ((Entity item, EntityListPanelItem panel) in _items) {
            if (item.IsDestroyed) {
                panel.RemoveFromParent();
                _items.Remove(item);
                continue;
            }

            panel.Update();
        }
    }
}