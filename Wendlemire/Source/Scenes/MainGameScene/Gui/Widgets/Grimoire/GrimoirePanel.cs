namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.Grimoire;

public sealed class GrimoirePanel : VerticalStackPanel, IUpdatable
{
    private readonly TabPanel _tabs;
    private readonly PawnInventory _inventory;
    private readonly List<CraftingPanel> _craftingPanels = [];

    public GrimoirePanel()
    {
        Padding = new Thickness(12);
        Height = 800;
        MinWidth = 900;
        Spacing = 0;

        _inventory = Core.Context.PlayerPawn.Inventory;
        _inventory.ItemAdded += OnInventoryChanged;
        _inventory.ItemRemoved += OnInventoryChanged;
        _inventory.ItemStackSizeChanged += OnInventoryChanged;

        _tabs = new TabPanel(tabsOnTop: true)
        {
            ButtonStyle = BaseContent.Styles.Button.Normal,
        };

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
        var incense = DefRepository<ItemDef>.Defs
            .Where(d => d is { ItemType: ItemType.Incense, CraftingProperties: not null })
            .OrderBy(d => d.Label)
            .ToList();

        if (cooking.Count > 0)
            AddCraftingTab($"Cooking ({cooking.Count})", "Cook", cooking);
        if (potions.Count > 0)
            AddCraftingTab($"Potions ({potions.Count})", "Brew", potions);
        if (medicinal.Count > 0)
            AddCraftingTab($"Medicinal ({medicinal.Count})", "Prepare", medicinal);
        if (supplies.Count > 0)
            AddCraftingTab($"Supplies ({supplies.Count})", "Craft", supplies);
        if (incense.Count > 0)
            AddCraftingTab($"Incense ({incense.Count})", "Prepare", incense);

        var cloakenatorDef = DefRepository<ItemDef>.GetByMoniker("Cloakenator");
        if (cloakenatorDef != null && Core.Context.Player.HasTrinket(cloakenatorDef))
        {
            var cloaks = DefRepository<ItemDef>.Defs
                .Where(d => d.EquipmentProperties?.SlotUsedToEquip == EquipmentSlotType.Cloak && d.CraftingProperties != null)
                .OrderBy(d => d.Label)
                .ToList();

            if (cloaks.Count > 0)
                AddCraftingTab($"Cloaks ({cloaks.Count})", "Weave", cloaks);
        }

        _tabs.AddTab("Disassemble", new DisassembleTab());

        Widgets.Add(_tabs);
        UpdateTabIndicators();
    }

    private void AddCraftingTab(string tabLabel, string buttonLabel, List<ItemDef> items)
    {
        var panel = new CraftingPanel(buttonLabel, items, Core.Context.PlayerPawn);
        _craftingPanels.Add(panel);
        _tabs.AddTab(tabLabel, panel);
    }

    private void OnInventoryChanged(Item _)
    {
        _tabs.Update();
        UpdateTabIndicators();
    }

    private void UpdateTabIndicators()
    {
        for (var i = 0; i < _craftingPanels.Count; i++)
        {
            _tabs.SetTabIndicator(i, _craftingPanels[i].HasCraftableRecipe);
        }
    }

    public void Detach()
    {
        _inventory.ItemAdded -= OnInventoryChanged;
        _inventory.ItemRemoved -= OnInventoryChanged;
        _inventory.ItemStackSizeChanged -= OnInventoryChanged;
    }

    public void Update()
    {
    }
}
