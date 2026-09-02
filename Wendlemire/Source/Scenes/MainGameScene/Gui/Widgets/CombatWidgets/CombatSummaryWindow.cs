namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatSummaryWindow : Window
{
    private const int BannerWidth = 468;
    private const int BannerHeight = 240;

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
    private static readonly Color Defeat = new(196, 90, 58);

    public event Action? OnReviewRequested;

    public CombatSummaryWindow(Encounter encounter, Action onContinue)
    {
        var handler = encounter.CombatHandler!;
        var playerWon = !handler.Player.IsDead;

        TitlePanel.Visible = false;
        MinWidth = 0;
        Padding = new Thickness(7);
        Background = new SolidBrush(FrameWood);
        Border = new SolidBrush(FrameOuter);
        BorderThickness = new Thickness(1);
        Content = playerWon
            ? BuildVictoryContent(encounter, handler, onContinue)
            : BuildDeathReportContent(encounter, handler, onContinue);
    }

    private Widget BuildVictoryContent(Encounter encounter, CombatHandler handler, Action onContinue)
    {
        var stats = BuildStatsGrid();
        AddStatRow(stats, 0, "Opponent", handler.Enemy.LabelShort);
        AddStatRow(stats, 1, "Duration", $"{encounter.Ticks} ticks");
        AddStatRow(stats, 2, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}", Color.Goldenrod);

        var rowIndex = 3;
        if (handler.CauseOfDeath != null)
        {
            AddStatRow(stats, rowIndex++, "Cause of Death", handler.CauseOfDeath);
        }

        if (handler.KillingWeapon != null)
        {
            AddStatRow(stats, rowIndex++, "Killing Blow", handler.KillingWeapon);
        }

        if (handler.KillingManeuver != null)
        {
            AddStatRow(stats, rowIndex, "Maneuver", handler.KillingManeuver);
        }

        var extras = new List<Widget>();
        if (handler.CollectedLoot.Count > 0)
        {
            extras.Add(BuildLootSection(handler));
        }

        if (BuildSeveredLabel(handler, "Limbs lost") is { } severed)
        {
            extras.Add(severed);
        }

        return BuildTablet(
            BuildBanner(BaseContent.Textures.VictorySplash, "VICTORY", Color.Goldenrod),
            stats,
            extras,
            BuildButtons("Continue", onContinue));
    }

    private Widget BuildDeathReportContent(Encounter encounter, CombatHandler handler, Action onContinue)
    {
        var deathRecords = Core.Context.DeathRecords.List;
        var totalDamage = deathRecords.Sum(r => r.TotalDamageDealt) + handler.TotalDirectPlayerDamage;

        var stats = BuildStatsGrid();
        AddStatRow(stats, 0, "Slain by", handler.Enemy.LabelShort, Color.OrangeRed);
        AddStatRow(stats, 1, "Location", encounter.Zone.ZoneDef.Label);
        AddStatRow(stats, 2, "Duration", $"{encounter.Ticks} ticks");
        AddStatRow(stats, 3, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}", Color.Goldenrod);
        AddStatRow(stats, 4, "Enemies Defeated", $"{deathRecords.Count}");
        AddStatRow(stats, 5, "Total Damage", $"{totalDamage:N0}", Color.Goldenrod);

        var extras = new List<Widget>();
        if (deathRecords.Count > 0)
        {
            extras.Add(BuildKillHistory(deathRecords));
        }

        if (BuildSeveredLabel(handler, "Limbs lost") is { } severed)
        {
            extras.Add(severed);
        }

        var continueText = Core.Context.ArenaRun != null || DebugSettings.TestSimMode ? "Continue" : "Try Again";
        return BuildTablet(
            BuildBanner(
                BaseContent.Textures.RunEndSplash,
                "DEFEAT",
                Defeat,
                handler.CauseOfDeath == null ? null : $"Killed by: {handler.CauseOfDeath}"),
            stats,
            extras,
            BuildButtons(continueText, () =>
            {
                if (DebugSettings.TestSimMode || Core.Context.ArenaRun != null)
                {
                    onContinue();
                    return;
                }

                Core.Context.StartOver();
            }));
    }

    private static Widget BuildBanner(Texture2D splash, string title, Color titleColor, string? subtitle = null)
    {
        var titleBlock = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleBlock.Widgets.Add(new Label
        {
            Text = title,
            Font = BaseContent.Fonts.Display.Large,
            TextColor = titleColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        titleBlock.Widgets.Add(new EmberRule { Width = 220, Height = 3, HorizontalAlignment = HorizontalAlignment.Center });
        if (subtitle != null)
        {
            titleBlock.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = subtitle,
                TextColor = Color.IndianRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Wrap = true,
                MaxWidth = BannerWidth - 32
            });
        }

        var banner = new Panel
        {
            Width = BannerWidth,
            Height = BannerHeight,
            ClipToBounds = true
        };
        banner.Widgets.Add(new CoverImage(splash));
        banner.Widgets.Add(new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidBrush(HeaderVeil),
            Padding = new Thickness(12, 8, 12, 10),
            Widgets = { titleBlock }
        });
        return banner;
    }

    private static Widget BuildTablet(Widget banner, Widget stats, IReadOnlyList<Widget> extras, Widget buttons)
    {
        var body = new VerticalStackPanel
        {
            Spacing = 8,
            Padding = new Thickness(16, 12, 16, 14),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = { stats }
        };
        foreach (var extra in extras)
        {
            body.Widgets.Add(extra);
        }

        body.Widgets.Add(buttons);

        return new Panel
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
    }

    private Widget BuildButtons(string continueText, Action onContinue)
    {
        var reviewButton = IronButton("Review Combat", 220);
        reviewButton.Click += (_, _) =>
        {
            Visible = false;
            OnReviewRequested?.Invoke();
        };

        var continueButton = IronButton(continueText, 220);
        continueButton.Click += (_, _) =>
        {
            Close();
            onContinue();
        };

        return new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
            Widgets = { reviewButton, continueButton }
        };
    }

    private static Widget BuildLootSection(CombatHandler handler)
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

            itemPanel.Widgets.Add(new Image
            {
                Background = resource.Item.GetIconImage(),
                Width = 48,
                Height = 48
            });

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

        return new VerticalStackPanel
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

    private static Widget BuildKillHistory(IReadOnlyList<DeathRecord> deathRecords)
    {
        var killList = new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        foreach (var record in deathRecords.TakeLast(6))
        {
            killList.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"#{record.Round}  {record.PawnName}  —  {record.CauseOfDeath}",
                TextColor = Dust,
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
        }

        return new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Kills",
                    TextColor = Dust,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                killList
            }
        };
    }

    private static Label? BuildSeveredLabel(CombatHandler handler, string prefix)
    {
        if (handler.SeveredLimbs.Count == 0)
        {
            return null;
        }

        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{prefix}: {string.Join(", ", handler.SeveredLimbs.Select(l => l.Label))}",
            TextColor = Color.OrangeRed,
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true,
            Width = BannerWidth - 32
        };
    }

    private static Grid BuildStatsGrid()
    {
        var stats = new Grid
        {
            RowSpacing = 4,
            ColumnSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            DefaultRowProportion = Proportion.Auto
        };
        stats.ColumnsProportions.Add(Proportion.Fill);
        stats.ColumnsProportions.Add(Proportion.Auto);
        return stats;
    }

    private static void AddStatRow(Grid grid, int row, string label, string value, Color? valueColor = null)
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
