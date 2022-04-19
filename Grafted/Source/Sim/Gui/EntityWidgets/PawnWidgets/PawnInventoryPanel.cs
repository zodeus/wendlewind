using System;
using System.Collections.ObjectModel;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.UI;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnInventoryPanel : VerticalStackPanel {
    private readonly EntityListPanel _potions;
    private readonly EntityListPanel _consumables;
    private readonly EntityListPanel _equipment;

    public PawnInventoryPanel(PawnInventory inventory, Action<Entity>? leftClickAction = null) {
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
        Proportions.Add(Proportion.Fill);

        _consumables = new EntityListPanel(
            inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType is ItemType.Medical or ItemType.TradeTool || entity.Def == Defs.Items.Cauterize,
            leftClickAction
        );
        _potions = new EntityListPanel(
            inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Potion,
            leftClickAction
        );
        _equipment = new EntityListPanel(
            inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Equipment,
            leftClickAction
        );
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Consumables" });
        AddChild(_consumables);

        AddChild(new HorizontalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Potions" });
        AddChild(new ScrollViewer() { Content = _potions, MaxHeight = 400, });

        AddChild(new HorizontalSeparator());
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Equipment" });
        AddChild(new ScrollViewer() { Content = _equipment, MaxHeight = 400 });

        ImageButton trash = new(BaseContent.Styles.Button.Icon) {
            Width = 48, Height = 48, Padding = new Thickness(6),
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Trash],
            VerticalAlignment = VerticalAlignment.Bottom
        };
        trash.Click += (_, _) => {
            Mouse.GetState();
            if (Input.LeftMouseButtonReleased && Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
                inventory.Items.Remove(item);
                item.Destroy();
                Core.Sim.Gui!.MouseAttachment.Detach();
            }
        };
        AddChild(trash);
    }


    public void Update() {
        _consumables.Update();
        _potions.Update();
        _equipment.Update();
    }
}