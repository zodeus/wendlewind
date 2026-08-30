namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakLootBoxCard : Panel
{
    private const int CardWidth = 340;
    private const int ChestIconSize = 96;
    private const int ItemIconSize = 36;

    public BoakLootBoxCard(LootBoxDef def)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
        Padding = new Thickness(0);
        Width = CardWidth;

        var content = new VerticalStackPanel { Spacing = 0 };

        // Header with chest icon and title
        var header = new HorizontalStackPanel
        {
            Spacing = 14,
            Padding = new Thickness(14),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };

        var chestIcon = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(6),
            Widgets =
            {
                new Image
                {
                    Width = ChestIconSize,
                    Height = ChestIconSize,
                    Background = new TextureRegion(def.GetIcon())
                }
            }
        };
        header.Widgets.Add(chestIcon);

        var headerInfo = new VerticalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        headerInfo.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = def.Label,
            Wrap = true,
            TextColor = BaseContent.Colors.Text.Golden
        });

        // Category badge
        var categoryColor = GetCategoryColor(def.Category);

        headerInfo.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = def.Category.ToString().ToUpper(),
            TextColor = categoryColor
        });

        header.Widgets.Add(headerInfo);
        content.Widgets.Add(header);

        // Body
        var body = new VerticalStackPanel
        {
            Spacing = 12,
            Padding = new Thickness(14)
        };

        // Stats row
        var statsRow = new HorizontalStackPanel
        {
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // Collection limit
        var limitText = def.CollectionLimit.Min == def.CollectionLimit.Max
            ? $"{def.CollectionLimit.Min}"
            : $"{def.CollectionLimit.Min}-{def.CollectionLimit.Max}";

        statsRow.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 6,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Pick:",
                    TextColor = new Color(140, 120, 90)
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = limitText,
                    TextColor = new Color(200, 200, 200)
                }
            }
        });

        // Trap indicator
        if (def.TrapProperties != null)
        {
            statsRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "⚠ TRAPPED",
                TextColor = new Color(220, 100, 80)
            });
        }

        body.Widgets.Add(statsRow);

        // Items section
        if (def.Items.Count > 0)
        {
            var itemsSection = new VerticalStackPanel { Spacing = 6 };

            itemsSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"CONTAINS ({def.Items.Count} items)",
                TextColor = new Color(140, 120, 90),
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Items grid - show items in rows of 7
            var currentRow = new HorizontalStackPanel { Spacing = 4 };
            var itemCount = 0;
            const int itemsPerRow = 7;

            foreach (var item in def.Items)
            {
                var itemContainer = new VerticalStackPanel
                {
                    Spacing = 2,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var iconPanel = new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    Padding = new Thickness(2),
                    Widgets =
                    {
                        new Image
                        {
                            Width = ItemIconSize,
                            Height = ItemIconSize,
                            Background = item.ItemDef.GetIconImage()
                        }
                    }
                };
                itemContainer.Widgets.Add(iconPanel);

                // Show chance if less than 100%
                if (item.ChanceToDrop < 1)
                {
                    itemContainer.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"{(int)(item.ChanceToDrop * 100)}%",
                        TextColor = new Color(180, 160, 100),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                else if (item.Amount.Max > 1)
                {
                    var amountText = item.Amount.Min == item.Amount.Max
                        ? $"x{item.Amount.Min}"
                        : $"{item.Amount.Min}-{item.Amount.Max}";
                    itemContainer.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = amountText,
                        TextColor = new Color(160, 160, 160),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }

                currentRow.Widgets.Add(itemContainer);
                itemCount++;

                if (itemCount % itemsPerRow == 0)
                {
                    itemsSection.Widgets.Add(currentRow);
                    currentRow = new HorizontalStackPanel { Spacing = 4 };
                }
            }

            // Add remaining items
            if (currentRow.Widgets.Count > 0)
            {
                itemsSection.Widgets.Add(currentRow);
            }

            body.Widgets.Add(itemsSection);
        }

        // Footer stats
        var footerStats = new HorizontalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };

        footerStats.Widgets.Add(CreateStatBadge($"{def.Items.Count}", "Items", new Color(120, 180, 120)));

        var guaranteedCount = def.Items.Count(i => i.ChanceToDrop >= 1);
        if (guaranteedCount > 0 && guaranteedCount < def.Items.Count)
        {
            footerStats.Widgets.Add(CreateStatBadge($"{guaranteedCount}", "Guaranteed", new Color(200, 180, 80)));
        }

        body.Widgets.Add(footerStats);

        content.Widgets.Add(body);
        Widgets.Add(content);
    }

    public static Color GetCategoryColor(LootBoxCategory category)
    {
        return category switch
        {
            LootBoxCategory.Weapons => new Color(200, 80, 80),
            LootBoxCategory.Armor => new Color(100, 140, 200),
            LootBoxCategory.Food => new Color(180, 140, 80),
            LootBoxCategory.Trinkets => new Color(180, 100, 180),
            LootBoxCategory.Medicinal => new Color(100, 180, 100),
            LootBoxCategory.Resources => new Color(200, 50, 50),
            LootBoxCategory.Supplies => new Color(50, 200, 50),
            LootBoxCategory.Potions => new Color(50, 50, 200),
            LootBoxCategory.Enchantments => new Color(200, 50, 200),
            _ => new Color(180, 180, 180)
        };
    }
    private static Widget CreateStatBadge(string value, string label, Color valueColor)
    {
        return new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = value,
                    TextColor = valueColor,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = label,
                    TextColor = new Color(120, 120, 120),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };
    }
}

internal sealed class BoakLootBoxPanel : ScrollViewer
{
    private const int CardsPerRow = 5;

    public BoakLootBoxPanel(IReadOnlyList<LootBoxDef> defs)
    {
        var mainContainer = new VerticalStackPanel
        {
            Spacing = 24,
            Margin = new Thickness(16)
        };

        // Group by category
        var groupedDefs = defs
            .GroupBy(d => d.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groupedDefs)
        {
            var section = CreateCategorySection(group.Key, group.OrderBy(d => d.Label).ToList());
            mainContainer.Widgets.Add(section);
        }

        Content = mainContainer;
    }

    private static Widget CreateCategorySection(LootBoxCategory category, List<LootBoxDef> defs)
    {
        var section = new VerticalStackPanel { Spacing = 12 };

        // Category header
        var header = new HorizontalStackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 4)
        };

        header.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = category.ToString().ToUpper(),
            TextColor = BoakLootBoxCard.GetCategoryColor(category)
        });

        header.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"({defs.Count} chests)",
            TextColor = new Color(120, 120, 120),
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 2)
        });

        section.Widgets.Add(header);

        // Divider line
        section.Widgets.Add(new Panel
        {
            Height = 2,
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new Myra.Graphics2D.Brushes.SolidBrush(BoakLootBoxCard.GetCategoryColor(category) * 0.5f)
        });

        // Cards grid
        var grid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 16,
            Margin = new Thickness(0, 8, 0, 0)
        };

        for (var i = 0; i < CardsPerRow; i++)
        {
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        }

        var row = 0;
        var col = 0;

        foreach (var def in defs)
        {
            var card = new BoakLootBoxCard(def);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, col);
            grid.Widgets.Add(card);

            col++;
            if (col >= CardsPerRow)
            {
                col = 0;
                row++;
            }
        }

        section.Widgets.Add(grid);

        return section;
    }
}
