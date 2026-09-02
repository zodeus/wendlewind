namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatSummaryWindow : Window
{
    private static readonly Color IronFill = new(42, 26, 20);
    private static readonly Color IronHover = new(58, 28, 20);
    private static readonly Color IronPressed = new(20, 12, 10);
    private static readonly Color IronEdge = new(60, 46, 38);
    private static readonly Color FrameOuter = new(10, 6, 4);
    private static readonly Color FrameWood = new(42, 22, 16);
    private static readonly Color FrameInset = new(106, 58, 40);
    private static readonly Color Rust = new(110, 42, 28);
    private static readonly Color Bone = new(203, 184, 150);
    private static readonly Color Dust = new(122, 110, 88);
    private static readonly Color HeaderVeil = new(7, 5, 4, 176);
    private static readonly Color TabletFill = new(16, 10, 8, 236);

    public event Action? OnReviewRequested;

    public CombatSummaryWindow(Encounter encounter, Action onContinue)
    {
        var handler = encounter.CombatHandler!;
        var playerWon = !handler.Player.IsDead;

        TitlePanel.Visible = false;

        if (playerWon)
        {
            MinWidth = 0;
            Padding = new Thickness(7);
            Background = new SolidBrush(FrameWood);
            Border = new SolidBrush(FrameOuter);
            BorderThickness = new Thickness(1);
            Content = BuildVictoryContent(encounter, handler, onContinue);
        }
        else
        {
            MinWidth = 500;
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
            Content = BuildDeathReportContent(encounter, handler, onContinue);
        }
    }

    private Widget BuildVictoryContent(Encounter encounter, CombatHandler handler, Action onContinue)
    {
        const int bannerWidth = 468;
        const int bannerHeight = 240;

        var banner = new Panel
        {
            Width = bannerWidth,
            Height = bannerHeight,
            ClipToBounds = true
        };
        banner.Widgets.Add(new CoverImage(BaseContent.Textures.VictorySplash));
        banner.Widgets.Add(new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidBrush(HeaderVeil),
            Padding = new Thickness(12, 8, 12, 10),
            Widgets =
            {
                new VerticalStackPanel
                {
                    Spacing = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        new Label
                        {
                            Text = "VICTORY",
                            Font = BaseContent.Fonts.Display.Large,
                            TextColor = Color.Goldenrod,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new EmberRule { Width = 220, Height = 3, HorizontalAlignment = HorizontalAlignment.Center }
                    }
                }
            }
        });

        var statsPanel = new Grid
        {
            RowSpacing = 4,
            ColumnSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            DefaultRowProportion = Proportion.Auto
        };
        statsPanel.ColumnsProportions.Add(Proportion.Fill);
        statsPanel.ColumnsProportions.Add(Proportion.Auto);

        AddVictoryStatRow(statsPanel, 0, "Opponent", handler.Enemy.LabelShort);
        AddVictoryStatRow(statsPanel, 1, "Duration", $"{encounter.Ticks} ticks");
        AddVictoryStatRow(statsPanel, 2, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}", Color.Goldenrod);

        var rowIndex = 3;
        if (handler.CauseOfDeath != null)
        {
            AddVictoryStatRow(statsPanel, rowIndex++, "Cause of Death", handler.CauseOfDeath);
        }

        if (handler.KillingWeapon != null)
        {
            AddVictoryStatRow(statsPanel, rowIndex++, "Killing Blow", handler.KillingWeapon);
        }

        if (handler.KillingManeuver != null)
        {
            AddVictoryStatRow(statsPanel, rowIndex, "Maneuver", handler.KillingManeuver);
        }

        Widget? lootSection = null;
        if (handler.CollectedLoot.Count > 0)
        {
            var lootItems = new HorizontalStackPanel
            {
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (var resource in handler.CollectedLoot.Take(8))
            {
                var itemPanel = new Panel
                {
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
                    Padding = new Thickness(3)
                };

                var itemIcon = new Image
                {
                    Background = resource.Item.GetIconImage(),
                    Width = 48,
                    Height = 48
                };
                itemPanel.Widgets.Add(itemIcon);

                if (resource.Count > 1)
                {
                    itemPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"x{resource.Count}",
                        TextColor = Bone,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom
                    });
                }

                itemPanel.WithTooltip(resource.Item.Label);
                lootItems.Widgets.Add(itemPanel);
            }

            if (handler.CollectedLoot.Count > 8)
            {
                lootItems.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"+{handler.CollectedLoot.Count - 8} more",
                    TextColor = Dust,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            lootSection = new VerticalStackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = "Loot",
                        TextColor = Dust,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    lootItems
                }
            };
        }

        Widget? severedSection = null;
        if (handler.SeveredLimbs.Count > 0)
        {
            severedSection = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Limbs lost: {string.Join(", ", handler.SeveredLimbs.Select(l => l.Label))}",
                TextColor = Color.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Wrap = true,
                Width = bannerWidth - 32
            };
        }

        var reviewButton = IronButton("Review Combat", 220);
        reviewButton.Click += (_, _) =>
        {
            Visible = false;
            OnReviewRequested?.Invoke();
        };

        var continueButton = IronButton("Continue", 220);
        continueButton.Click += (_, _) =>
        {
            Close();
            onContinue();
        };

        var buttons = new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
            Widgets = { reviewButton, continueButton }
        };

        var body = new VerticalStackPanel
        {
            Spacing = 8,
            Padding = new Thickness(16, 12, 16, 14),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = { statsPanel }
        };

        if (lootSection != null)
        {
            body.Widgets.Add(lootSection);
        }

        if (severedSection != null)
        {
            body.Widgets.Add(severedSection);
        }

        body.Widgets.Add(buttons);

        var tablet = new Panel
        {
            Background = new SolidBrush(TabletFill),
            Border = new SolidBrush(FrameInset),
            BorderThickness = new Thickness(1),
            Widgets =
            {
                new VerticalStackPanel
                {
                    Spacing = 0,
                    Widgets = { banner, body }
                }
            }
        };

        return tablet;
    }

    private Widget BuildDeathReportContent(Encounter encounter, CombatHandler handler, Action onContinue)
    {
        var deathRecords = Core.Context.DeathRecords.List;
        var totalDamageAllRuns = deathRecords.Sum(r => r.TotalDamageDealt) + handler.TotalDirectPlayerDamage;
        var totalKills = deathRecords.Count;

        // Title
        var title = new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "DEATH REPORT",
            TextColor = new Color(180, 30, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Subtitle with cause of death
        var causeLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"Killed by: {handler.CauseOfDeath ?? "Unknown"}",
            TextColor = Color.IndianRed,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // Final Combat Stats Section
        var finalCombatTitle = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "— FINAL BATTLE —",
            TextColor = Color.DarkGray,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 8)
        };

        var finalStatsPanel = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddStatRow(finalStatsPanel, 0, "Slain by", handler.Enemy.LabelShort, Color.OrangeRed);
        AddStatRow(finalStatsPanel, 1, "Location", encounter.Zone.ZoneDef.Label, Color.LightGray);
        AddStatRow(finalStatsPanel, 2, "Combat Duration", $"{encounter.Ticks} ticks", Color.LightGray);
        AddStatRow(finalStatsPanel, 3, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}", Color.Goldenrod);

        // Run Summary Section
        var runTitle = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "— RUN SUMMARY —",
            TextColor = Color.DarkGray,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 8)
        };

        var runStatsPanel = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddStatRow(runStatsPanel, 0, "Enemies Defeated", $"{totalKills}", Color.Goldenrod);
        AddStatRow(runStatsPanel, 1, "Total Damage", $"{totalDamageAllRuns:N0}", Color.Goldenrod);

        // Kill History Section (last 5 kills)
        Widget? killHistorySection = null;
        if (deathRecords.Count > 0)
        {
            var killHistoryTitle = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = "— KILL HISTORY —",
                TextColor = Color.DarkGray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 8)
            };

            var killList = new VerticalStackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (var record in deathRecords.TakeLast(6))
            {
                var killEntry = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"#{record.Round} {record.PawnName} — {record.CauseOfDeath}",
                    TextColor = Color.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                killList.Widgets.Add(killEntry);
            }

            killHistorySection = new VerticalStackPanel
            {
                Widgets = { killHistoryTitle, killList }
            };
        }

        // Severed limbs
        Widget? severedSection = null;
        if (handler.SeveredLimbs.Count > 0)
        {
            var severedLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Limbs lost in final battle: {string.Join(", ", handler.SeveredLimbs.Select(l => l.Label))}",
                TextColor = Color.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 0),
                Wrap = true,
                Width = 450
            };
            severedSection = severedLabel;
        }

        // Restart button
        var restartButton = new CursorButton(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = "Try Again", HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 30, 0, 0)
        };
        if (Core.Context.ArenaRun != null)
        {
            restartButton.Content = new Label { Text = "Continue", HorizontalAlignment = HorizontalAlignment.Center };
        }

        restartButton.Click += (_, _) =>
        {
            Close();
            if (DebugSettings.TestSimMode || Core.Context.ArenaRun != null)
            {
                onContinue();
            }
            else
            {
                Core.Context.StartOver();
            }
        };

        // Build content
        var content = new VerticalStackPanel
        {
            Spacing = 0,
            Padding = new Thickness(50, 35, 50, 35),
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                title,
                causeLabel,
                finalCombatTitle,
                finalStatsPanel,
                runTitle,
                runStatsPanel
            }
        };

        if (killHistorySection != null)
            content.Widgets.Add(killHistorySection);

        if (severedSection != null)
            content.Widgets.Add(severedSection);

        content.Widgets.Add(restartButton);

        return content;
    }

    private static void AddVictoryStatRow(Grid grid, int row, string label, string value, Color? valueColor = null)
    {
        var labelWidget = new Label(BaseContent.Styles.Label.Small)
        {
            Text = label,
            TextColor = Dust
        };
        Grid.SetRow(labelWidget, row);
        Grid.SetColumn(labelWidget, 0);
        grid.Widgets.Add(labelWidget);

        var valueWidget = new Label(BaseContent.Styles.Label.Small)
        {
            Text = value,
            TextColor = valueColor ?? Bone,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(valueWidget, row);
        Grid.SetColumn(valueWidget, 1);
        grid.Widgets.Add(valueWidget);
    }

    private static void AddStatRow(Grid grid, int row, string label, string value, Color? valueColor = null)
    {
        var labelWidget = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = label + ":",
            TextColor = Color.LightGray
        };
        Grid.SetRow(labelWidget, row);
        Grid.SetColumn(labelWidget, 0);
        grid.Widgets.Add(labelWidget);

        var valueWidget = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = value,
            TextColor = valueColor ?? Color.White
        };
        Grid.SetRow(valueWidget, row);
        Grid.SetColumn(valueWidget, 1);
        grid.Widgets.Add(valueWidget);
    }

    private static CursorButton IronButton(string text, int width)
    {
        var button = new CursorButton
        {
            Content = new Label
            {
                Text = text,
                Font = BaseContent.Fonts.Display.Small,
                TextColor = Bone,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            Width = width,
            Padding = new Thickness(12, 8),
            Background = new SolidBrush(IronFill),
            OverBackground = new SolidBrush(IronHover),
            PressedBackground = new SolidBrush(IronPressed),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1)
        };
        button.MouseEntered += (_, _) => button.Border = new SolidBrush(Rust);
        button.MouseLeft += (_, _) => button.Border = new SolidBrush(IronEdge);
        return button;
    }

    private sealed class CoverImage : Widget
    {
        private readonly Texture2D _texture;

        public CoverImage(Texture2D texture)
        {
            _texture = texture;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ClipToBounds = true;
        }

        public override void InternalRender(RenderContext context)
        {
            base.InternalRender(context);
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0 || _texture.Width <= 0 || _texture.Height <= 0)
            {
                return;
            }

            var scale = Math.Max(bounds.Width / (float)_texture.Width, bounds.Height / (float)_texture.Height);
            var width = (int)MathF.Ceiling(_texture.Width * scale);
            var height = (int)MathF.Ceiling(_texture.Height * scale);
            context.Draw(_texture, new Rectangle(
                bounds.X + (bounds.Width - width) / 2,
                bounds.Y + (bounds.Height - height) / 2,
                width,
                height), Color.White);
        }
    }

    private sealed class EmberRule : Widget
    {
        private static Texture2D? _pixel;

        public override void InternalRender(RenderContext context)
        {
            base.InternalRender(context);
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var pixel = Pixel();
            var y = bounds.Y + bounds.Height / 2;
            for (var x = 0; x < bounds.Width; x++)
            {
                var t = x / (float)Math.Max(1, bounds.Width - 1);
                var edge = t < 0.5f ? t * 2f : (1f - t) * 2f;
                var color = Color.Lerp(Rust, new Color(201, 160, 112), 1f - MathF.Abs(t - 0.5f) * 2f);
                color *= 0.35f + edge * 0.65f;
                context.Draw(pixel, new Rectangle(bounds.X + x, y, 1, bounds.Height), color);
            }
        }

        private static Texture2D Pixel()
        {
            if (_pixel != null)
            {
                return _pixel;
            }

            _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
            return _pixel;
        }
    }
}
