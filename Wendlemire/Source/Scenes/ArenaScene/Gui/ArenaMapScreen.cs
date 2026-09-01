using Wendlemire.NetCode.Contracts;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.MapWidgets;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaMapScreen : Grid
{
    private const int PortraitSize = 360;
    private const int ResultsWidth = 360;
    private static readonly Color TitleColor = new(214, 208, 196);
    private static readonly Color BodyColor = new(168, 164, 156);
    private static readonly Color Completed = new(80, 140, 80);
    private static readonly Color Current = new(232, 170, 0);
    private static readonly Color Locked = new(50, 50, 55);
    private static readonly Color PanelFill = new(18, 14, 12);
    private static readonly Color PanelEdge = new(96, 48, 32);

    private readonly GameContext _context;
    private readonly GoldPurse _purse;
    private readonly Label _runStats;

    public ArenaMapScreen(
        GameContext context,
        Action<MerchantDef> onMerchantPicked,
        CombatResult? lastFight = null)
    {
        Padding = new Thickness(24, 16);
        ColumnSpacing = 0;
        RowSpacing = 16;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Fill));

        _context = context;
        var run = context.ArenaRun ?? throw new InvalidOperationException("Map requires an ArenaRun.");
        if (run.CurrentMerchant == null)
        {
            run.AssignNextMerchant();
        }

        var merchant = run.CurrentMerchant
                       ?? throw new InvalidOperationException("Map requires a rolled merchant.");
        _purse = new GoldPurse(context);
        _runStats = new Label(BaseContent.Styles.Label.Medium)
        {
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };
        RefreshRunStats(run);

        var header = new VerticalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new HorizontalStackPanel
                {
                    Spacing = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets = { _purse, _runStats }
                },
                BuildFightSpine(run)
            }
        };

        Widgets.Add(header);
        var reveal = BuildMerchantReveal(run, merchant, lastFight, onMerchantPicked);
        Widgets.Add(reveal);
        Grid.SetRow(reveal, 1);
    }

    private static Widget BuildFightSpine(ArenaRun run)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        for (var i = 1; i <= ArenaRun.WinsToFinish; i++)
        {
            if (i > 1)
            {
                var reached = i - 1 <= run.Wins;
                row.Widgets.Add(new Panel
                {
                    Width = 22,
                    Height = 3,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidBrush(reached ? Completed : Locked)
                });
            }

            var state = i <= run.Wins
                ? MapNodeState.Completed
                : i == run.Wins + 1
                    ? MapNodeState.Current
                    : MapNodeState.Locked;
            var color = state switch
            {
                MapNodeState.Completed => Completed,
                MapNodeState.Current => Current,
                _ => Locked
            };
            var size = state == MapNodeState.Current ? 42 : 34;

            row.Widgets.Add(new Panel
            {
                Width = size,
                Height = size,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64],
                    color),
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = i.ToString(),
                        TextColor = state == MapNodeState.Locked ? BodyColor : Color.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            });
        }

        return row;
    }

    private static Widget BuildMerchantReveal(
        ArenaRun run,
        MerchantDef merchant,
        CombatResult? lastFight,
        Action<MerchantDef> onMerchantPicked)
    {
        var visit = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label
            {
                Text = "Visit shop",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Center
        };
        visit.Click += (_, _) => onMerchantPicked(merchant);

        var pair = new Grid
        {
            ColumnSpacing = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        pair.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        pair.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        pair.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var merchantColumn = CreateMerchantColumn(merchant, onMerchantPicked);
        var results = CreateResultsPanel(run, merchant, lastFight);
        pair.Widgets.Add(merchantColumn);
        pair.Widgets.Add(results);
        Grid.SetColumn(results, 1);

        return new VerticalStackPanel
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { pair, visit }
        };
    }

    private static Widget CreateMerchantColumn(MerchantDef merchant, Action<MerchantDef> onPicked)
    {
        var portrait = CreatePortrait(merchant);
        var button = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = portrait,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0)
        };
        button.Click += (_, _) => onPicked(merchant);

        return new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = $"The {merchant.Label}",
                    TextColor = TitleColor,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                button,
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = ShelfSummary(merchant),
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };
    }

    private static Widget CreateResultsPanel(ArenaRun run, MerchantDef merchant, CombatResult? lastFight)
    {
        var won = run.LastFightWon;
        var body = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = won ? "Victory" : "Defeat",
            TextColor = won ? Color.Goldenrod : Color.IndianRed,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        body.Widgets.Add(DetailRow("Reward", $"+{run.LastGoldDelta}g", Color.LightGreen));
        body.Widgets.Add(DetailRow("Purse", $"{run.Gold}g", Color.Goldenrod));
        body.Widgets.Add(DetailRow("Record", $"{run.Wins} wins · {run.Losses} losses"));
        body.Widgets.Add(DetailRow("Lives", $"{run.LivesRemaining} remaining"));
        body.Widgets.Add(DetailRow("Next fight", $"{run.Wins + 1} of {ArenaRun.WinsToFinish}"));

        if (lastFight is { Ticks: > 0 })
        {
            var seconds = lastFight.Ticks / (float)GameContext.TicksPerSecond;
            body.Widgets.Add(DetailRow("Duration", $"{seconds:0}s"));
        }

        var cause = lastFight?.CauseOfDeath;
        if (!string.IsNullOrWhiteSpace(cause))
        {
            body.Widgets.Add(DetailRow(won ? "They died of" : "You died of", cause));
        }

        var opponent = lastFight?.Defender?.PawnName;
        if (!string.IsNullOrWhiteSpace(opponent))
        {
            body.Widgets.Add(DetailRow("Opponent", opponent));
        }

        if (!string.IsNullOrWhiteSpace(merchant.Description))
        {
            body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = merchant.Description,
                TextColor = BodyColor,
                Wrap = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            });
        }

        return new Panel
        {
            Width = ResultsWidth,
            MinHeight = PortraitSize,
            Padding = new Thickness(20, 16),
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidBrush(PanelFill),
            Border = new SolidBrush(PanelEdge),
            BorderThickness = new Thickness(2),
            Widgets = { body }
        };
    }

    private static Widget DetailRow(string label, string value, Color? valueColor = null)
    {
        return new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = label,
                    TextColor = BodyColor,
                    Width = 110
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = value,
                    TextColor = valueColor ?? TitleColor,
                    Wrap = true
                }
            }
        };
    }

    private static Widget CreatePortrait(MerchantDef merchant)
    {
        IImage? portrait = null;
        if (!string.IsNullOrWhiteSpace(merchant.TexturePath)
            && Core.Content.TryLoad<Texture2D>(merchant.TexturePath, out var texture)
            && texture != null)
        {
            portrait = new TextureRegion(texture);
        }

        var frame = new Panel
        {
            Width = PortraitSize,
            Height = PortraitSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright]
        };

        if (portrait != null)
        {
            frame.Widgets.Add(new Image
            {
                Background = portrait,
                Width = PortraitSize - 16,
                Height = PortraitSize - 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            return frame;
        }

        frame.Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = merchant.Label.Length > 0 ? merchant.Label[0].ToString() : "?",
            TextColor = Color.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        return frame;
    }

    private static string ShelfSummary(MerchantDef merchant)
    {
        var labels = merchant.Shelves
            .Select(shelf => shelf.Category.Label())
            .Distinct()
            .ToList();
        return labels.Count == 0 ? merchant.Kind.ToString() : string.Join(" · ", labels);
    }

    private void RefreshRunStats(ArenaRun run)
    {
        _runStats.Text = $"Wins {run.Wins}/{ArenaRun.WinsToFinish}   Lives {run.LivesRemaining}";
    }

    public void Update()
    {
        _purse.Refresh();
        if (_context.ArenaRun != null)
        {
            RefreshRunStats(_context.ArenaRun);
        }
    }
}
