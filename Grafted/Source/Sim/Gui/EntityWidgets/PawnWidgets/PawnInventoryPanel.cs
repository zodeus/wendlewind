using System;
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
    private readonly Pawn _pawn;
    private readonly EntityListPanel _potions;
    private readonly EntityListPanel _consumables;
    private readonly EntityListPanel _equipment;
    private readonly EntityListPanel _resources;
    private Label _weightLabel;

    public PawnInventoryPanel(Pawn pawn, Action<Entity>? leftClickAction = null, Action<Entity>? rightClickAction = null) {
        _pawn = pawn;
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
            pawn.Inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType is ItemType.Medical or ItemType.TradeTool || entity.Def == Defs.Items.Cauterize,
            leftClickAction,
            rightClickAction
        );
        _potions = new EntityListPanel(
            pawn.Inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Potion,
            leftClickAction,
            rightClickAction
        );
        _equipment = new EntityListPanel(
            pawn.Inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Equipment,
            leftClickAction,
            rightClickAction
        );
        _resources = new EntityListPanel(
            pawn.Inventory.Items,
            entity => ((Item) entity).ItemDef.ItemType == ItemType.Resource,
            leftClickAction,
            rightClickAction
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
        ImageButton trash = new(BaseContent.Styles.Button.Icon) {
            Width = 48, Height = 48, Padding = new Thickness(6),
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Trash]
        };
        trash.Click += (_, _) => {
            Mouse.GetState();
            if (Input.LeftMouseButtonReleased && Core.Sim.Gui!.MouseAttachment?.Data is Item item) {
                pawn.Inventory.Items.Remove(item);
                item.Destroy();
                Core.Sim.Gui!.MouseAttachment.Detach();
            }
        };
        AddChild(new HorizontalStackPanel {
            Widgets = { trash, _weightLabel },
            Spacing = 30,
            VerticalAlignment = VerticalAlignment.Bottom
        });
    }


    public void Update() {
        _consumables.Update();
        _potions.Update();
        _equipment.Update();
        _resources.Update();
        _weightLabel.Text = $"Weight {_pawn.Inventory.Weight}/{_pawn.MaxCarryWeight}";
    }
}