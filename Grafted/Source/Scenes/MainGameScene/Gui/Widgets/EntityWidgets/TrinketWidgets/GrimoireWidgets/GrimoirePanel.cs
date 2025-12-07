namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets.GrimoireWidgets;

[UsedImplicitly]
public sealed class GrimoirePanel : EntityPanelBase
{
    private readonly TabPanel _tabs;
    
    public GrimoirePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(12);
        //Width = 1000;
        Height = 800;
        Spacing = 0;
        
        // Create tab panel with styled tabs at top
        _tabs = new TabPanel(tabsOnTop: true)
        {
            ButtonStyle = BaseContent.Styles.Button.Normal,
        };
        
        // Gather craftable items by category
        var cooking = DefRepository<ItemDef>.Defs
            .Where(d => d is { ItemType: ItemType.Food, CraftingProperties: not null })
            .OrderBy(d => d.Label)
            .ToList();
        var potions = DefRepository<ItemDef>.Defs
            .Where(d => d is { ItemType: ItemType.Potion, CraftingProperties: not null })
            .OrderBy(d => d.Label)
            .ToList();
        var medicinal = DefRepository<ItemDef>.Defs
            .Where(d => d is { ItemType: ItemType.Medical, CraftingProperties: not null })
            .OrderBy(d => d.Label)
            .ToList();
        var supplies = DefRepository<ItemDef>.Defs
            .Where(d => d is { ItemType: ItemType.Supplies, CraftingProperties: not null })
            .OrderBy(d => d.Label)
            .ToList();
        var flammables = DefRepository<ItemDef>.Defs
            .Where(d => d is { ItemType: ItemType.Flammable, CraftingProperties: not null })
            .OrderBy(d => d.Label)
            .ToList();
        
        // Add tabs with category-specific action verbs
        if (cooking.Count > 0)
            _tabs.AddTab($"Cooking ({cooking.Count})", new CraftingPanel("Cook", cooking, Core.Context.PlayerPawn));
        if (potions.Count > 0)
            _tabs.AddTab($"Potions ({potions.Count})", new CraftingPanel("Brew", potions, Core.Context.PlayerPawn));
        if (medicinal.Count > 0)
            _tabs.AddTab($"Medicinal ({medicinal.Count})", new CraftingPanel("Prepare", medicinal, Core.Context.PlayerPawn));
        if (supplies.Count > 0)
            _tabs.AddTab($"Supplies ({supplies.Count})", new CraftingPanel("Craft", supplies, Core.Context.PlayerPawn));
        if (flammables.Count > 0)
            _tabs.AddTab($"Flammables ({flammables.Count})", new CraftingPanel("Create", flammables, Core.Context.PlayerPawn));
        
        Widgets.Add(_tabs);
    }

    public override void Update()
    {
    }
}
