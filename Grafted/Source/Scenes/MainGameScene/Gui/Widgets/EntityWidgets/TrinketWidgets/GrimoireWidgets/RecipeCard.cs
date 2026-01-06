using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets.GrimoireWidgets;

public class RecipeCard : Panel
{
    private readonly string _buttonLabel;
    private readonly Panel _emptyStatePanel;
    private readonly HorizontalStackPanel _contentPanel;
    
    private Pawn? _currentPawn;
    private ItemDef? _currentItem;
    public ItemDef? CurrentItem => _currentItem;

    public RecipeCard(string buttonLabel)
    {
        _buttonLabel = buttonLabel;
        Padding = new Thickness(24);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Background = new SolidBrush(new Color(20, 16, 12));

        // Empty state panel - shown when no recipe is selected
        _emptyStatePanel = CreateEmptyState();

        // Content panel - two-column layout shown when a recipe is selected
        _contentPanel = new HorizontalStackPanel
        {
            Spacing = 24,
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
        };

        Widgets.Add(_emptyStatePanel);
        Widgets.Add(_contentPanel);
    }

    private Panel CreateEmptyState()
    {
        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        
        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
        };
        
        var iconLabel = new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "?",
            TextColor = new Color(80, 70, 60),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        
        var hintLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Select a recipe to view details",
            TextColor = new Color(120, 100, 80),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        
        content.Widgets.Add(iconLabel);
        content.Widgets.Add(hintLabel);
        panel.Widgets.Add(content);
        
        return panel;
    }

    public void SetItem(Pawn pawn, ItemDef itemDef)
    {
        _currentPawn = pawn;
        _currentItem = itemDef;
        
        ClearCard();
        
        _emptyStatePanel.Visible = false;
        _contentPanel.Visible = true;

        // LEFT COLUMN: Item info + button (fixed height so button doesn't move)
        var leftColumn = new VerticalStackPanel
        {
            Spacing = 12,
            Width = 300,
            Height = 320, // Fixed height so button position is consistent
        };
        
        leftColumn.Widgets.Add(CreateHeaderSection(itemDef));
        
        if (!string.IsNullOrEmpty(itemDef.Description))
        {
            leftColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = itemDef.Description,
                TextColor = new Color(160, 150, 140),
                Wrap = true,
                MaxWidth = 280,
            });
        }
        
        leftColumn.Widgets.Add(CreateAmountSection(itemDef));
        
        // Spacer to push button to bottom of fixed-height column
        var spacer = new Panel { Height = 1 };
        VerticalStackPanel.SetProportionType(spacer, ProportionType.Fill);
        leftColumn.Widgets.Add(spacer);
        
        // Button always at bottom of left column
        leftColumn.Widgets.Add(CreateCraftButton(pawn, itemDef));
        
        // RIGHT COLUMN: Requirements (can be any height)
        var rightColumn = new VerticalStackPanel
        {
            Spacing = 16,
            Width = 420,
        };
        
        rightColumn.Widgets.Add(CreateIngredientsSection(pawn, itemDef));
        
        if (itemDef.CraftingProperties?.RequiredTrinkets?.Count > 0)
        {
            rightColumn.Widgets.Add(CreateTrinketsSection(pawn, itemDef));
        }
        
        // Vertical divider
        var divider = new Panel
        {
            Width = 2,
            Background = new SolidBrush(new Color(50, 45, 40)),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        
        _contentPanel.Widgets.Add(leftColumn);
        _contentPanel.Widgets.Add(divider);
        _contentPanel.Widgets.Add(rightColumn);
    }

    private Widget CreateHeaderSection(ItemDef itemDef)
    {
        var header = new HorizontalStackPanel
        {
            Spacing = 12,
        };
        
        var iconFrame = new Panel
        {
            Width = 72,
            Height = 72,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(itemDef.Texture),
            Width = 64,
            Height = 64,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        
        var nameLabel = new Label(BaseContent.Styles.Label.Large)
        {
            Text = itemDef.Label,
            TextColor = BaseContent.Colors.Text.Golden,
            VerticalAlignment = VerticalAlignment.Center,
            Wrap = true,
            MaxWidth = 200,
        };
        
        header.Widgets.Add(iconFrame);
        header.Widgets.Add(nameLabel);
        
        return header;
    }

    private Widget CreateAmountSection(ItemDef itemDef)
    {
        var panel = new HorizontalStackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };
        
        var yieldLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Yield:",
            TextColor = new Color(140, 130, 120),
        };
        
        var amountBadge = new Panel
        {
            Background = new SolidBrush(new Color(40, 70, 40)),
            Padding = new Thickness(10, 4, 10, 4),
        };
        amountBadge.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"×{itemDef.CraftingProperties?.AmountProduced ?? 1}",
            TextColor = new Color(140, 230, 140),
        });
        
        panel.Widgets.Add(yieldLabel);
        panel.Widgets.Add(amountBadge);
        
        // Show current inventory count
        var ownedCount = _currentPawn?.Inventory.AmountOf(itemDef) ?? 0;
        
        var ownedLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Owned:",
            TextColor = new Color(140, 130, 120),
            Margin = new Thickness(16, 0, 0, 0),
        };
        
        var ownedBadge = new Panel
        {
            Background = new SolidBrush(new Color(50, 50, 70)),
            Padding = new Thickness(10, 4, 10, 4),
        };
        ownedBadge.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"{ownedCount}",
            TextColor = ownedCount > 0 ? new Color(180, 180, 220) : new Color(120, 120, 140),
        });
        
        panel.Widgets.Add(ownedLabel);
        panel.Widgets.Add(ownedBadge);
        
        return panel;
    }

    private Widget CreateCraftButton(Pawn pawn, ItemDef itemDef)
    {
        var canCraft = itemDef.CraftingProperties?.CanCraft(pawn) == true;
        
        var container = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        var button = new CursorButton(canCraft ? BaseContent.Styles.Button.LargeGold : BaseContent.Styles.Button.Large)
        {
            Enabled = canCraft,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        button.Content = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = _buttonLabel,
            TextColor = canCraft ? BaseContent.Colors.Text.Golden : new Color(80, 80, 80),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        
        button.Click += (_, _) =>
        {
            if (_currentPawn != null && _currentItem != null && CraftItem(_currentPawn, _currentItem))
            {
                SetItem(_currentPawn, _currentItem);
            }
        };
        
        if (!canCraft)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Missing requirements",
                TextColor = new Color(160, 90, 90),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
        }
        
        container.Widgets.Add(button);
        
        return container;
    }

    private Widget CreateIngredientsSection(Pawn pawn, ItemDef itemDef)
    {
        var section = new VerticalStackPanel { Spacing = 8 };
        
        section.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = "Ingredients",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 4),
        });
        
        var requirements = itemDef.CraftingProperties?.ResourceRequirements ?? [];
        if (requirements.Count == 0)
        {
            section.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "No ingredients required",
                TextColor = new Color(100, 100, 100),
            });
        }
        else
        {
            foreach (var itemCount in requirements)
            {
                section.Widgets.Add(CreateIngredientRow(pawn, itemCount));
            }
        }
        
        return section;
    }

    private Widget CreateIngredientRow(Pawn pawn, ResourceCount itemCount)
    {
        var hasEnough = pawn.Inventory.AmountOf(itemCount.Item) >= itemCount.Count;
        var currentAmount = pawn.Inventory.AmountOf(itemCount.Item);
        
        var row = new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        var iconFrame = new Panel
        {
            Width = 36,
            Height = 36,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark32],
            Padding = new Thickness(2),
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(itemCount.Item.Texture),
            Width = 24,
            Height = 24,
            Opacity = hasEnough ? 1.0f : 0.5f,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        
        var nameLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = itemCount.Item.Label,
            TextColor = hasEnough ? new Color(200, 200, 200) : new Color(160, 90, 90),
            VerticalAlignment = VerticalAlignment.Center,
        };
        
        var countColor = hasEnough ? $"/c[{TC.Green}]" : $"/c[{TC.Red}]";
        var countLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"{countColor}{currentAmount}/c[#666666]/{itemCount.Count}",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        
        var statusIcon = new Image
        {
            Background = Stylesheet.Current.Atlas[hasEnough 
                ? BaseContent.Styles.Atlas.Icon.Checkmark 
                : BaseContent.Styles.Atlas.Icon.X],
            Width = 18,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center,
        };
        
        row.Widgets.Add(iconFrame);
        row.Widgets.Add(nameLabel);
        HorizontalStackPanel.SetProportionType(nameLabel, ProportionType.Fill);
        row.Widgets.Add(countLabel);
        row.Widgets.Add(statusIcon);
        
        return row;
    }

    private Widget CreateTrinketsSection(Pawn pawn, ItemDef itemDef)
    {
        var section = new VerticalStackPanel { Spacing = 8 };
        
        section.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = "Required Trinkets",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 4),
        });
        
        foreach (var trinket in itemDef.CraftingProperties?.RequiredTrinkets ?? [])
        {
            section.Widgets.Add(CreateTrinketRow(pawn, trinket));
        }
        
        return section;
    }

    private Widget CreateTrinketRow(Pawn pawn, ItemDef trinketDef)
    {
        var hasTrinket = pawn.Inventory.Trinkets.Any(t => t.Def == trinketDef);
        
        var row = new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        
        var iconFrame = new Panel
        {
            Width = 36,     
            Height = 36,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark32],
            Padding = new Thickness(2),
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(trinketDef.Texture),
            Width = 24,
            Height = 24,
            Opacity = hasTrinket ? 1.0f : 0.4f,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        
        var nameLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = trinketDef.Label,
            TextColor = hasTrinket ? new Color(140, 230, 140) : new Color(160, 90, 90),
            VerticalAlignment = VerticalAlignment.Center,
        };
        
        var statusIcon = new Image
        {
            Background = Stylesheet.Current.Atlas[hasTrinket 
                ? BaseContent.Styles.Atlas.Icon.Checkmark 
                : BaseContent.Styles.Atlas.Icon.X],
            Width = 18,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center,
        };
        
        row.Widgets.Add(iconFrame);
        row.Widgets.Add(nameLabel);
        HorizontalStackPanel.SetProportionType(nameLabel, ProportionType.Fill);
        row.Widgets.Add(statusIcon);
        
        return row;
    }

    private bool CraftItem(Pawn pawn, ItemDef itemToCraft)
    {
        List<Item> resourcesTaken = [];
        foreach (var resource in itemToCraft.CraftingProperties!.ResourceRequirements)
        {
            var resourceToUse = pawn.Inventory.Take(resource);

            if (resourceToUse == null)
            {
                foreach (var resourceTaken in resourcesTaken)
                {
                    pawn.Inventory.TryAdd(resourceTaken);
                }

                return false;
            }

            resourcesTaken.Add(resourceToUse);
            if (resourceToUse.StackSize < resource.Count)
            {
                return false;
            }
        }

        foreach (var resourceTaken in resourcesTaken)
        {
            resourceTaken.Destroy();
        }

        pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(itemToCraft, itemToCraft.CraftingProperties.AmountProduced));

        return true;
    }

    private void ClearCard()
    {
        _contentPanel.Widgets.Clear();
    }

    public void Update()
    {
        // Refresh the display if an item is currently selected
        if (_currentPawn != null && _currentItem != null)
        {
            SetItem(_currentPawn, _currentItem);
        }
    }
}
