using System;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class ItemContainerPanel : VerticalStackPanel {
    private readonly ItemContainer _container;
    private readonly ItemContainer? _receivingContainer;

    private readonly EntityListPanel _potions;
    private readonly EntityListPanel _consumables;
    private readonly EntityListPanel _equipment;
    private readonly EntityListPanel _resources;
    private Label _weightLabel;

    public ItemContainerPanel(ItemContainer container, ItemContainer? receivingContainer) {
        _container = container;
        _receivingContainer = receivingContainer;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(30);
        Spacing = 10;
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Fill);

        _consumables = new EntityListPanel(
            _container,
            entity => ((Item) entity).ItemDef.ItemType is ItemType.Medical or ItemType.TradeTool || entity.Def == Defs.Items.Cauterize,
            LeftClickHandler,
            RightClickHandler
        );
        _potions = new EntityListPanel(
            _container,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Potion,
            LeftClickHandler,
            RightClickHandler
        );
        _equipment = new EntityListPanel(
            _container,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Equipment,
            LeftClickHandler,
            RightClickHandler
        );
        _resources = new EntityListPanel(
            _container,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Resource,
            LeftClickHandler,
            RightClickHandler
        );
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Consumables" });
        AddChild(_consumables);

        AddChild(new HorizontalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Potions" });
        AddChild(new ScrollViewer() { Content = _potions, MaxHeight = 200, });

        AddChild(new HorizontalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Equipment" });
        AddChild(new ScrollViewer() { Content = _equipment, MaxHeight = 400 });

        AddChild(new HorizontalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Resources" });
        AddChild(new ScrollViewer() { Content = _resources, MaxHeight = 300 });

        _weightLabel = new Label(BaseContent.Styles.Label.Large) {
            VerticalAlignment = VerticalAlignment.Bottom
        };
        ImageButton trash = TrashButton();
        AddChild(new HorizontalStackPanel {
            Widgets = { _weightLabel },
            Spacing = 30,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right
        });
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
            if (item.Def.Moniker == "Cauterize") {
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

    private ImageButton TrashButton() {
        ImageButton trash = new(BaseContent.Styles.Button.Icon) {
            Width = 48, Height = 48, Padding = new Thickness(6),
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Trash]
        };
        trash.Click += (_, _) => {
            if (Input.LeftMouseButtonReleased && Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
                _container.Remove(item);
                item.Destroy();
                Core.Sim.Gui!.MouseAttachment.Detach();
            }
        };
        return trash;
    }


    public void Update() {
        _consumables.Update();
        _potions.Update();
        _equipment.Update();
        _resources.Update();
        _weightLabel.Text = $"Weight {_container.Weight}/{_container.MaxWeight}";
    }
}