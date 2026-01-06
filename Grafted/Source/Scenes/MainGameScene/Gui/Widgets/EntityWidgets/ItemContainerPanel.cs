namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class ItemContainerPanel : Panel
{
    private readonly BaseGui _gui;

    private readonly List<InventoryListPanel> _sections = new();

    public ItemContainerPanel(BaseGui gui, PawnInventory inventory)
    {
        _gui = gui;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame], new Color(100, 100, 100, 220));
        Padding = new Thickness(15, 15, 15, 15);

        List<ItemContainerPanelSection> sections = new()
        {
            new ItemContainerPanelSection
            {
                Label = "Medicinal",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Medical || entity.Def == Defs.Items.Cauterize
            },
            new ItemContainerPanelSection
            {
                Label = "Potions",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Potion
            },
            new ItemContainerPanelSection
            {
                Label = "Food",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Food
            },
            new ItemContainerPanelSection
            {
                Label = "Flammable",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Flammable
            },
            new ItemContainerPanelSection
            {
                Label = "Equipment Supplies",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Supplies
            },
            new ItemContainerPanelSection
            {
                Label = "Equipment",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType == ItemType.Equipment
            },
            new ItemContainerPanelSection
            {
                Label = "Enchantments",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Enchantment
            },
            new ItemContainerPanelSection
            {
                Label = "Resources",
                Inventory = inventory,
                Filter = entity => ((Item)entity).ItemDef.ItemType is ItemType.Resource
            }
        };

        var verticalStackPanel = new VerticalStackPanel();
        Widgets.Add(new ScrollViewer
        {
            ScrollMultiplier = 40,
            Content = verticalStackPanel
        });
        foreach (var section in sections)
        {
            InventoryListPanel panel = new(_gui, section.Label, section.Inventory, section.Filter)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _sections.Add(panel);
            verticalStackPanel.Widgets.Add(panel);
        }
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
        public PawnInventory Inventory { get; set; } = null!;
        public Func<Entity, bool>? Filter { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}