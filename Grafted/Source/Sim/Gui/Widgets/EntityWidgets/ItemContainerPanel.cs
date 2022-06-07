using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui.Widgets.MiscWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets;

public class ItemContainerPanel : VerticalStackPanel {
    private readonly EntityContainer _container;
    private readonly EntityContainer? _receivingContainer;

    private readonly List<EntityListPanel> _sections = new();
    private readonly Label _weightLabel;

    public ItemContainerPanel(EntityContainer container, EntityContainer? receivingContainer) {
        _container = container;
        _receivingContainer = receivingContainer;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(30);
        Spacing = 10;

        List<ItemContainerPanelSection> sections = new() {
            new ItemContainerPanelSection {
                Label = "Consumables",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType is ItemType.Medical or ItemType.TradeTool || entity.Def == Defs.Items.Cauterize
            },
            new ItemContainerPanelSection {
                Label = "Potions",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType is ItemType.Potion
            },
            new ItemContainerPanelSection {
                Label = "Equipment",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType == ItemType.Equipment
            },
            new ItemContainerPanelSection {
                Label = "Resources & Trinkets",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType is ItemType.Resource or ItemType.Trinket
            }
        };


        foreach (ItemContainerPanelSection section in sections) {
            Proportions.Add(Proportion.Auto);
            Proportions.Add(Proportion.Auto);
            Proportions.Add(Proportion.Auto);

            EntityListPanel panel = new(section.Container, section.Filter, LeftClickHandler, RightClickHandler) {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _sections.Add(panel);
            AddChild(new HorizontalSeparator());
            AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = section.Label });
            AddChild(new ScrollViewer { Content = panel, MaxHeight = 200 });
        }

        Proportions.Add(Proportion.Fill);
        _weightLabel = new Label(BaseContent.Styles.Label.Large) {
            VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right
        };
        AddChild(_weightLabel);
    }

    private void RightClickHandler(Entity entity) {
        if (entity is not Item item) {
            return;
        }

        if (item.Def.Moniker == "Cauterize") {
            return;
        }

        if (Input.IsKeyDown(Keys.LeftShift)) {
            _receivingContainer?.TryAdd(item, 1);
        }
        else {
            _receivingContainer?.TryAdd(item);
        }

    }

    private void LeftClickHandler(Entity entity) {
        if (entity is not Item item) {
            return;
        }

        // Shift + Left-Click to delete item 
        if (Core.Sim.Gui!.MouseAttachment == null && Input.IsKeyDown(Keys.LeftControl)) {
            if (item.CanBeDestroyed == false) {
                return;
            }

            _container.Remove(item);
            item.Destroy();
            return;
        }

        Core.Sim.Gui!.MouseAttachment = new MouseAttachment(
            item.Icon,
            updateAction: attachment => {
                if (Input.RightMouseButtonPressed) attachment.Detach();
            }
        ) {
            IconSize = new Size(40, 40),
            Data = item,
        };
    }

    public void Update() {
        foreach (EntityListPanel section in _sections) {
            section.Update();
        }

        _weightLabel.Text = $"{_container.Weight}/{_container.MaxWeight}";
    }

    private class ItemContainerPanelSection {
        public EntityContainer Container { get; set; }
        public Func<Entity, bool>? Filter { get; set; }
        public string Label { get; set; }
    }
}