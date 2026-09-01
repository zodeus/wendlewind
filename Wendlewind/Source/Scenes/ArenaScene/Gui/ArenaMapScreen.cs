using Wendlewind.Scenes.MainGameScene.Gui.Widgets.MapWidgets;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaMapScreen : Grid
{
    private const int PortraitSize = 360;
    private static readonly Color TitleColor = new(214, 208, 196);
    private static readonly Color BodyColor = new(168, 164, 156);
    private static readonly Color Completed = new(80, 140, 80);
    private static readonly Color Current = new(232, 170, 0);
    private static readonly Color Locked = new(50, 50, 55);

    private readonly GameContext _context;
    private readonly GoldPurse _purse;
    private readonly Label _runStats;

    public ArenaMapScreen(GameContext context, Action<MerchantDef> onMerchantPicked)
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
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = "Choose a merchant",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextColor = TitleColor
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"Fight {run.Wins + 1} of {ArenaRun.WinsToFinish} — who stocks your next kit?",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextColor = BodyColor
                },
                BuildFightSpine(run)
            }
        };

        Widgets.Add(header);
        var merchants = BuildMerchantRow(onMerchantPicked);
        Widgets.Add(merchants);
        Grid.SetRow(merchants, 1);
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

    private static Widget BuildMerchantRow(Action<MerchantDef> onMerchantPicked)
    {
        var merchants = DefRepository<MerchantDef>.Defs.Where(m => !m.IsGeneralStore).ToList();
        var row = new Grid
        {
            ColumnSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        row.RowsProportions.Add(new Proportion(ProportionType.Auto));
        foreach (var _ in merchants)
        {
            row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        }

        for (var i = 0; i < merchants.Count; i++)
        {
            var captured = merchants[i];
            var card = CreateMerchantCard(captured, onMerchantPicked);
            row.Widgets.Add(card);
            Grid.SetColumn(card, i);
        }

        return row;
    }

    private static Widget CreateMerchantCard(MerchantDef merchant, Action<MerchantDef> onPicked)
    {
        var body = new Grid
        {
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        body.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        body.RowsProportions.Add(new Proportion(ProportionType.Auto));
        body.RowsProportions.Add(new Proportion(ProportionType.Auto));
        body.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var portrait = CreatePortrait(merchant);
        var name = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = merchant.Label,
            TextColor = TitleColor,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var shelves = new Label(BaseContent.Styles.Label.Small)
        {
            Text = ShelfSummary(merchant),
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        body.Widgets.Add(portrait);
        Grid.SetRow(portrait, 0);
        body.Widgets.Add(name);
        body.Widgets.Add(shelves);
        Grid.SetRow(name, 1);
        Grid.SetRow(shelves, 2);

        var button = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = body,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(12)
        };
        button.Click += (_, _) => onPicked(merchant);
        return button;
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
