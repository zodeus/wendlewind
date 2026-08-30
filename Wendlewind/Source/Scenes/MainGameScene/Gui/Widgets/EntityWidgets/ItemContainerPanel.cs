namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class ItemContainerPanel : VerticalStackPanel, IUpdatable
{
    private static readonly (string Label, ItemType Type)[] Categories =
    [
        ("Equipment", ItemType.Equipment),
        ("Trinkets", ItemType.Trinket),
        ("Medicinal", ItemType.Medical),
        ("Potions", ItemType.Potion),
        ("Food", ItemType.Food),
        ("Incense", ItemType.Incense),
        ("Supplies", ItemType.Supplies),
        ("Enchantments", ItemType.Enchantment),
        ("Resources", ItemType.Resource)
    ];

    private readonly TabPanel _tabs;

    public ItemContainerPanel(BaseGui gui, PawnInventory inventory)
    {
        Padding = new Thickness(12);
        Width = 1040;
        Height = 720;
        MinWidth = 1040;

        _tabs = new TabPanel(tabsOnTop: true)
        {
            ButtonStyle = BaseContent.Styles.Button.Normal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        foreach (var (label, type) in Categories)
        {
            _tabs.AddTab(label, new InventoryListPanel(gui, inventory, item => item.ItemDef.ItemType == type)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
        }

        Widgets.Add(_tabs);
        SetProportionType(_tabs, ProportionType.Fill);
        Update();
    }

    public void Update()
    {
        _tabs.Update();
    }
}
