namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ChainLinkPanel : EntityPanelBase
{    private readonly Label _chainLinkCountLabel;
    private readonly List<ArmorCraftButton> _craftButtons = [];
    private static readonly ItemDef ChainLinkDef = DefRepository<ItemDef>.GetByMoniker("ChainLink")!;

    public ChainLinkPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(24);
        MinWidth = 480;
        Spacing = 12;

        // Find all armor items that can be crafted with ChainLink
        var chainArmorRecipes = DefRepository<ItemDef>.Defs
            .Where(d => d.CraftingProperties?.ResourceRequirements
                .Any(r => r.Item == ChainLinkDef) == true)
            .Select(d => new ArmorRecipe(d, GetChainLinkCost(d)))
            .OrderBy(r => r.Cost)
            .ToList();

        // ═══════════════════════════════════════════════════════════════════
        // Header Section: Icon + Title + Chain Link Count
        // ═══════════════════════════════════════════════════════════════════
        var headerSection = new HorizontalStackPanel { Spacing = 16, Margin = new Thickness(0, 0, 0, 8) };

        // Icon with decorative frame
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(6),
            Width = 88, Height = 88
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
            Width = 76, Height = 76,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        // Title and resource count
        var titleArea = new VerticalStackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        titleArea.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = "Chain Armor Forge",
            TextColor = BaseContent.Colors.Text.Golden
        });

        var countPanel = new HorizontalStackPanel { Spacing = 8 };
        countPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Available:" });
        _chainLinkCountLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = GetChainLinkCount().ToString(),
            TextColor = new Color(120, 200, 255)
        };
        countPanel.Widgets.Add(_chainLinkCountLabel);
        titleArea.Widgets.Add(countPanel);

        headerSection.Widgets.Add(titleArea);
        Widgets.Add(headerSection);

        // Divider
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 8) });

        // ═══════════════════════════════════════════════════════════════════
        // Description
        // ═══════════════════════════════════════════════════════════════════
        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Forge chain mail armor from collected chain links. Each piece provides solid physical protection.",
            Wrap = true,
            TextColor = Color.LightGray,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // ═══════════════════════════════════════════════════════════════════
        // Armor Crafting Row
        // ═══════════════════════════════════════════════════════════════════
        var armorRow = new HorizontalStackPanel { Spacing = 12 };

        foreach (var recipe in chainArmorRecipes)
        {
            var craftButton = CreateArmorCraftButton(recipe);
            armorRow.Widgets.Add(craftButton.Container);
            _craftButtons.Add(craftButton);
        }

        Widgets.Add(armorRow);

        // Initial state update
        UpdateCraftButtons();
    }

    private static int GetChainLinkCost(ItemDef armorDef)
    {
        var chainLinkRequirement = armorDef.CraftingProperties?.ResourceRequirements
            .FirstOrDefault(r => r.Item == ChainLinkDef);
        return chainLinkRequirement?.Count ?? 0;
    }

    private ArmorCraftButton CreateArmorCraftButton(ArmorRecipe recipe)
    {
        var container = new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Armor icon button
        var button = new CursorButton(BaseContent.Styles.Button.Dark)
        {
            Padding = new Thickness(8),
            Content = new Image
            {
                Background = recipe.ArmorDef.GetIconImage(),
                Width = 64,
                Height = 64
            }
        };

        // Label with name and cost
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = recipe.ArmorDef.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 80,
            Wrap = true
        };

        var costLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"({recipe.Cost})",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Gray
        };

        container.Widgets.Add(button);
        container.Widgets.Add(label);
        container.Widgets.Add(costLabel);

        var craftButton = new ArmorCraftButton(container, button, recipe);

        button.Click += (_, _) => CraftArmor(craftButton);

        return craftButton;
    }

    private void CraftArmor(ArmorCraftButton craftButton)
    {
        var pawn = Core.Context.PlayerPawn;
        var recipe = craftButton.Recipe;

        if (!recipe.ArmorDef.CraftingProperties!.CanCraft(pawn)) return;

        // Take the chain links
        var resource = pawn.Inventory.Take(ChainLinkDef, recipe.Cost);
        resource?.Destroy();

        // Create the armor (use AmountProduced from CraftingProperties)
        var amountProduced = recipe.ArmorDef.CraftingProperties.AmountProduced;
        var armor = Core.Context.Factory.CreateEntity<Item>(recipe.ArmorDef, amountProduced);
        pawn.Inventory.TryAdd(armor);

        // Update UI
        UpdateCraftButtons();

        // Show feedback
        Gui.PushScreenMessage(new ScreenMessageData
        {
            Text = $"Forged {recipe.ArmorDef.Label}!",
            Duration = 3,
            Color = BaseContent.Colors.Text.Golden
        });
    }

    private int GetChainLinkCount()
    {
        return Core.Context.PlayerPawn.Inventory.AmountOf(ChainLinkDef);
    }

    private void UpdateCraftButtons()
    {
        var available = GetChainLinkCount();
        _chainLinkCountLabel.Text = available.ToString();
        _chainLinkCountLabel.TextColor = available > 0 ? new Color(120, 200, 255) : new Color(180, 60, 60);

        var pawn = Core.Context.PlayerPawn;
        foreach (var craftButton in _craftButtons)
        {
            var canCraft = craftButton.Recipe.ArmorDef.CraftingProperties?.CanCraft(pawn) == true;
            craftButton.Button.Enabled = canCraft;
            craftButton.Button.Opacity = canCraft ? 1.0f : 0.4f;
        }
    }

    public override void Update()
    {
        UpdateCraftButtons();
    }

    private record ArmorRecipe(ItemDef ArmorDef, int Cost);

    private record ArmorCraftButton(Widget Container, CursorButton Button, ArmorRecipe Recipe);
}
