namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaShopScreen : Grid
{
    private readonly GameContext _context;
    private readonly ArenaHud _hud;
    private readonly Label _status;

    public ArenaShopScreen(GameContext context, Action onDone)
    {
        _context = context;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Padding = new Thickness(16, 12);
        ColumnSpacing = 0;
        RowSpacing = 8;
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(new Proportion(ProportionType.Auto));

        var run = context.ArenaRun ?? throw new InvalidOperationException("Shop requires an ArenaRun.");
        var merchant = run.CurrentMerchant
                       ?? DefRepository<MerchantDef>.GetByMoniker("GeneralStore")
                       ?? throw new InvalidOperationException("No merchant selected.");

        _hud = new ArenaHud(context);
        _status = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Buy what you want, then continue. Leftover gold is fine.",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Gray
        };

        var header = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                _hud,
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = merchant.Label,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextColor = Color.Goldenrod
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = merchant.Description,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Wrap = true
                },
                _status
            }
        };

        var stock = ShopStock.Roll(merchant, run.RunSeed, run.Wins);
        var grid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        for (var c = 0; c < 5; c++)
        {
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        }

        for (var i = 0; i < stock.Count; i++)
        {
            var card = CreateOfferCard(stock[i]);
            grid.Widgets.Add(card);
            Grid.SetColumn(card, i % 5);
            Grid.SetRow(card, i / 5);
            if (grid.RowsProportions.Count <= i / 5)
            {
                grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }
        }

        var catalog = new ScrollViewer
        {
            Content = grid,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var done = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label
            {
                Text = "Continue",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 240
        };
        done.Click += (_, _) => onDone();

        Add(header, 0);
        Add(catalog, 1);
        Add(done, 2);
    }

    private void Add(Widget widget, int row)
    {
        Widgets.Add(widget);
        Grid.SetRow(widget, row);
    }

    private Widget CreateOfferCard(MerchantOffer offer)
    {
        var buy = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{offer.GoldCost}g",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        buy.Click += (_, _) => TryBuy(offer);

        return new VerticalStackPanel
        {
            Width = 150,
            Spacing = 4,
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = offer.ItemDef.Label,
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Wrap = true,
                    Width = 130
                },
                new Image
                {
                    Background = offer.ItemDef.GetIconImage(),
                    Width = 64,
                    Height = 64,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                buy
            }
        };
    }

    private void TryBuy(MerchantOffer offer)
    {
        if (_context.ArenaRun!.TryBuy(_context, offer))
        {
            _status.TextColor = Color.LightGreen;
            _status.Text = $"Bought {offer.ItemDef.Label}";
            _hud.Refresh();
            return;
        }

        _status.TextColor = Color.IndianRed;
        _status.Text = $"Cannot buy {offer.ItemDef.Label} ({offer.GoldCost}g, you have {_context.ArenaRun.Gold}g)";
    }

    public void Update()
    {
        _hud.Refresh();
    }
}
