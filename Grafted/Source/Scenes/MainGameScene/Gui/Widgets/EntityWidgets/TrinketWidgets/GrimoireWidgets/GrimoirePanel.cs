namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets.GrimoireWidgets;

[UsedImplicitly]
public sealed class GrimoirePanel : EntityPanelBase
{
    public GrimoirePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 400;
        MaxWidth = 1200;
        Spacing = 5;
        var tabs = new TabPanel(tabsOnTop: false) { ButtonStyle = BaseContent.Styles.Button.Small };
        var cooking = DefRepository<ItemDef>.Defs.Where(d => d is { ItemType: ItemType.Food, CraftingProperties: not null }).ToList();
        var potions = DefRepository<ItemDef>.Defs.Where(d => d is { ItemType: ItemType.Potion, CraftingProperties: not null }).ToList();
        var medicinal = DefRepository<ItemDef>.Defs.Where(d => d is { ItemType: ItemType.Medical, CraftingProperties: not null }).ToList();
        var supplies = DefRepository<ItemDef>.Defs.Where(d => d is { ItemType: ItemType.Supplies, CraftingProperties: not null }).ToList();
        tabs.AddTab("Cooking", new CraftingPanel("Cook", cooking, Core.Context.PlayerPawn));
        tabs.AddTab("Potions", new CraftingPanel("Brew", potions, Core.Context.PlayerPawn));
        tabs.AddTab("Medicinal", new CraftingPanel("Craft", medicinal, Core.Context.PlayerPawn));
        tabs.AddTab("Supplies", new CraftingPanel("Craft", supplies, Core.Context.PlayerPawn));
        Widgets.Add(tabs);
    }

    public override void Update()
    {
    }
}