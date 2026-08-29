using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakZoneCard : Panel
{
    private const int CardWidth = 430;
    private const int ImageHeight = 160;
    private const int IconSize = 40;

    public BoakZoneCard(ZoneDef zoneDef)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
        Padding = new Thickness(0);
        Width = CardWidth;

        var content = new VerticalStackPanel { Spacing = 0 };

        // Zone background image as header
        var imageContainer = new Panel
        {
            Height = ImageHeight,
            ClipToBounds = true,
            Widgets =
            {
                new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new TextureRegion(zoneDef.GetBackground())
                }
            }
        };

        // Stage badge overlay
        var stageBadge = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"Stage {zoneDef.Stage}",
                    TextColor = new Color(200, 200, 200)
                }
            }
        };
        imageContainer.Widgets.Add(stageBadge);

        // Biome color indicator strip
        var colorStrip = new Panel
        {
            Height = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidBrush(zoneDef.ZoneColor)
        };
        imageContainer.Widgets.Add(colorStrip);

        content.Widgets.Add(imageContainer);

        // Card body
        var body = new VerticalStackPanel
        {
            Spacing = 12,
            Padding = new Thickness(14)
        };

        // Zone name
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = zoneDef.Label,
            TextColor = BaseContent.Colors.Text.Golden,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Encounters section
        if (zoneDef.Encounters.Count > 0)
        {
            var encountersSection = new VerticalStackPanel { Spacing = 6 };

            encountersSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "ENCOUNTERS",
                TextColor = new Color(140, 120, 90),
                Margin = new Thickness(0, 0, 0, 2)
            });

            var enemyGrid = new Grid
            {
                ColumnSpacing = 6,
                RowSpacing = 4
            };
            enemyGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            enemyGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            var row = 0;
            foreach (var encounter in zoneDef.Encounters)
            {
                var enemy = encounter.Enemies.FirstOrDefault();
                if (enemy == null) continue;

                var isBoss = encounter.IsBoss;
                var nameColor = isBoss ? new Color(220, 80, 80) : new Color(200, 200, 200);
                var prefix = isBoss ? "★ " : "• ";

                var enemyLabel = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"{prefix}{enemy.PawnName}",
                    TextColor = nameColor
                };
                Grid.SetRow(enemyLabel, row);
                Grid.SetColumn(enemyLabel, 0);
                enemyGrid.Widgets.Add(enemyLabel);

                row++;
            }

            encountersSection.Widgets.Add(enemyGrid);
            body.Widgets.Add(encountersSection);
        }

        // Resources section
        if (zoneDef.Resources.Count > 0)
        {
            var resourcesSection = new VerticalStackPanel { Spacing = 6 };

            resourcesSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "RESOURCES",
                TextColor = new Color(140, 120, 90),
                Margin = new Thickness(0, 0, 0, 2)
            });

            var resourcesFlow = new HorizontalStackPanel { Spacing = 8 };

            foreach (var resource in zoneDef.Resources.Take(6))
            {
                var resourceIcon = new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    Padding = new Thickness(4),
                    Widgets =
                    {
                        new Image
                        {
                            Width = IconSize,
                            Height = IconSize,
                            Background = new TextureRegion(resource.Item.GetIcon())
                        }
                    }
                };

                resourcesFlow.Widgets.Add(resourceIcon);
            }

            if (zoneDef.Resources.Count > 6)
            {
                resourcesFlow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"+{zoneDef.Resources.Count - 6}",
                    TextColor = new Color(140, 140, 140),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            resourcesSection.Widgets.Add(resourcesFlow);
            body.Widgets.Add(resourcesSection);
        }

        // Enemy drops section
        var allEnemyDrops = zoneDef.Encounters
            .SelectMany(e => e.Enemies)
            .SelectMany(enemy => enemy.InventoryItems)
            .Select(drop => drop.Item)
            .Distinct()
            .ToList();

        if (allEnemyDrops.Count > 0)
        {
            var dropsSection = new VerticalStackPanel { Spacing = 6 };

            dropsSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "ENEMY DROPS",
                TextColor = new Color(140, 120, 90),
                Margin = new Thickness(0, 0, 0, 2)
            });

            var dropsFlow = new HorizontalStackPanel { Spacing = 6 };

            foreach (var drop in allEnemyDrops.Take(7))
            {
                var dropIcon = new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                    Padding = new Thickness(3),
                    Widgets =
                    {
                        new Image
                        {
                            Width = 32,
                            Height = 32,
                            Background = new TextureRegion(drop.GetIcon())
                        }
                    }
                };

                dropsFlow.Widgets.Add(dropIcon);
            }

            if (allEnemyDrops.Count > 7)
            {
                dropsFlow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"+{allEnemyDrops.Count - 7}",
                    TextColor = new Color(140, 140, 140),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            dropsSection.Widgets.Add(dropsFlow);
            body.Widgets.Add(dropsSection);
        }

        // Lootboxes section
        var lootBoxCounts = zoneDef.Encounters
            .SelectMany(e => e.PotentialLootBoxes)
            .GroupBy(lb => lb.Label)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .ToList();

        if (lootBoxCounts.Count > 0)
        {
            var lootSection = new VerticalStackPanel { Spacing = 6 };

            lootSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "LOOT CHESTS",
                TextColor = new Color(140, 120, 90),
                Margin = new Thickness(0, 0, 0, 2)
            });

            var lootFlow = new VerticalStackPanel { Spacing = 4 };

            foreach (var (label, count) in lootBoxCounts)
            {
                lootFlow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"• {label} x{count}",
                    TextColor = new Color(200, 200, 200)
                });
            }

            lootSection.Widgets.Add(lootFlow);
            body.Widgets.Add(lootSection);
        }

        // Weather section
        if (zoneDef.Weathers.Count > 0)
        {
            var weatherSection = new VerticalStackPanel { Spacing = 6 };

            weatherSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "WEATHER",
                TextColor = new Color(140, 120, 90),
                Margin = new Thickness(0, 0, 0, 2)
            });

            var weatherFlow = new HorizontalStackPanel { Spacing = 8 };
            foreach (var weather in zoneDef.Weathers)
            {
                weatherFlow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = weather.Label,
                    TextColor = weather.DisplayColor
                });
            }

            weatherSection.Widgets.Add(weatherFlow);
            body.Widgets.Add(weatherSection);
        }

        // Stats footer
        var statsRow = new HorizontalStackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };

        statsRow.Widgets.Add(CreateStatBadge($"{zoneDef.Encounters.Where(e => e.MysteryProperties != null).Count()}", "Mysteries", new Color(120, 120, 210)));
        statsRow.Widgets.Add(CreateStatBadge($"{zoneDef.Encounters.Where(e => e.MysteryProperties == null).Count()}", "Battles", new Color(200, 120, 120)));
        statsRow.Widgets.Add(CreateStatBadge($"{zoneDef.Resources.Count}", "Drops", new Color(120, 200, 120)));
        statsRow.Widgets.Add(CreateStatBadge($"{lootBoxCounts.Count}", "Chests", new Color(200, 160, 80)));

        var bossCount = zoneDef.Encounters.Count(e => e.IsBoss);
        if (bossCount > 0)
        {
            statsRow.Widgets.Add(CreateStatBadge($"{bossCount}", "Boss", new Color(220, 180, 80)));
        }

        body.Widgets.Add(statsRow);

        content.Widgets.Add(body);
        Widgets.Add(content);
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

internal sealed class BoakZonePanel : ScrollViewer
{
    public BoakZonePanel(IReadOnlyList<ZoneDef> defs)
    {
        const int cardsPerRow = 4;
        var grid = new Grid
        {
            ColumnSpacing = 20,
            RowSpacing = 20,
            Margin = new Thickness(20)
        };

        for (var i = 0; i < cardsPerRow; i++)
        {
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        }

        var row = 0;
        var col = 0;
        foreach (var zoneDef in defs.OrderBy(z => z.Stage))
        {
            var card = new BoakZoneCard(zoneDef);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, col);
            grid.Widgets.Add(card);

            col++;
            if (col >= cardsPerRow)
            {
                col = 0;
                row++;
            }
        }

        Content = grid;
    }
}
