using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class ItemContainerPanel : Panel
{
    private readonly BaseGui _gui;
    private readonly EntityContainer _container;
    private readonly EntityContainer? _receivingContainer;

    private readonly List<EntityListPanel> _sections = new();

    public ItemContainerPanel(BaseGui gui, EntityContainer container, EntityContainer? receivingContainer = null)
    {
        _gui = gui;
        _container = container;
        _receivingContainer = receivingContainer;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(15, 15,15,15);

        List<ItemContainerPanelSection> sections = new()
        {
            new ItemContainerPanelSection
            {
                Label = "Medicinal",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Medical || entity.Def == Defs.Items.Cauterize
            },
            new ItemContainerPanelSection
            {
                Label = "Potions",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Potion
            },
            new ItemContainerPanelSection
            {
                Label = "Food",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Food
            },
            new ItemContainerPanelSection
            {
                Label = "Flammable",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Flammable
            },
            new ItemContainerPanelSection
            {
                Label = "Equipment Supplies",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Supplies
            },
            new ItemContainerPanelSection
            {
                Label = "Equipment",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType == ItemType.Equipment
            },
            new ItemContainerPanelSection
            {
                Label = "Enchantments",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Enchantment
            },
            new ItemContainerPanelSection
            {
                Label = "Resources",
                Container = _container,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Resource
            }
        };
        
        var verticalStackPanel = new VerticalStackPanel();
        Widgets.Add(new ScrollViewer
        {
            Content = verticalStackPanel
        });
        foreach (var section in sections)
        {
            EntityListPanel panel = new(_gui, section.Label, section.Container, section.Filter, LeftClickHandler, RightClickHandler)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _sections.Add(panel);
            verticalStackPanel.Widgets.Add(panel);
        }
    }

    private void RightClickHandler(Entity entity)
    {
        if (entity is not Item item)
        {
            return;
        }

        if (item.Def.Moniker == "Cauterize")
        {
            return;
        }

        /*if (Input.IsKeyDown(Keys.LeftShift))
        {
            _receivingContainer?.TryAdd(item, 1);
        }
        else
        {
            _receivingContainer?.TryAdd(item);
        }*/
    }

    private void LeftClickHandler(Entity entity)
    {
        if (entity is not Item item)
        {
            return;
        }

        // Shift + Left-Click to delete item 
        if (_gui.MouseAttachment == null && Keyboard.GetState().IsKeyDown(Keys.LeftShift))
        {
            if (item.CanBeDestroyed == false)
            {
                return;
            }

            _container.Remove(item);
            item.Destroy();
            return;
        }

        _gui.MouseAttachment = new MouseAttachment(
            _gui,
            item.Icon,
            updateAction: attachment =>
            {
                if (Mouse.GetState().RightButton == ButtonState.Pressed) attachment.Detach();
            }
        )
        {
            IconSize = new Size(40, 40),
            Data = item,
        };
    }

    public void Update()
    {
        foreach (var section in _sections)
        {
            section.Update();
        }
    }

    private class ItemContainerPanelSection
    {
        public EntityContainer Container { get; set; } = null!;
        public Func<Entity, bool>? Filter { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}