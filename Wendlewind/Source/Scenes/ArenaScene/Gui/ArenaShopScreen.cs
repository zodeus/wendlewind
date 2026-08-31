namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaShopScreen : Grid
{
    private readonly GameContext _context;
    private readonly GoldPurse _purse;
    private readonly Label _status;
    private readonly Label _runStats;
    private readonly HorizontalStackPanel _packRow;

    public ArenaShopScreen(GameContext context, Action onDone)
    {
        _context = context;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Padding = new Thickness(16, 12);
        ColumnSpacing = 0;
        RowSpacing = 10;
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(new Proportion(ProportionType.Auto));

        var run = context.ArenaRun ?? throw new InvalidOperationException("Shop requires an ArenaRun.");
        var merchant = run.CurrentMerchant
                       ?? DefRepository<MerchantDef>.GetByMoniker("GeneralStore")
                       ?? throw new InvalidOperationException("No merchant selected.");

        _purse = new GoldPurse(context);
        _status = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Buy from the shelves, or sell from your pack at a tenth of value.",
            HorizontalAlignment = HorizontalAlignment.Left,
            TextColor = Color.Gray,
            Wrap = true
        };
        _runStats = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };
        RefreshRunStats();

        var header = new Grid
        {
            ColumnSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        header.Widgets.Add(CreatePortrait(merchant));
        var info = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = merchant.Label,
                    TextColor = Color.Goldenrod
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = merchant.Description,
                    Wrap = true
                },
                new HorizontalStackPanel
                {
                    Spacing = 16,
                    Widgets = { _purse, _runStats }
                },
                _status
            }
        };
        header.Widgets.Add(info);
        Grid.SetColumn(info, 1);

        var shelves = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        foreach (var shelf in ShopStock.Roll(merchant, run.RunSeed, run.FightsPlayed))
        {
            shelves.Widgets.Add(CreateShelf(shelf));
        }

        var catalog = new ScrollViewer
        {
            Content = shelves,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _packRow = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        RebuildPack();

        var packPanel = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(10, 8),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = "Your pack",
                    TextColor = Color.Goldenrod
                },
                new ScrollViewer
                {
                    Content = _packRow,
                    ShowHorizontalScrollBar = true,
                    ShowVerticalScrollBar = false,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 130
                }
            }
        };

        var done = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label
            {
                Text = "Continue",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 200
        };
        done.Click += (_, _) => onDone();

        var footer = new Grid
        {
            ColumnSpacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        footer.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        footer.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        footer.Widgets.Add(packPanel);
        footer.Widgets.Add(done);
        Grid.SetColumn(done, 1);

        Add(header, 0);
        Add(catalog, 1);
        Add(footer, 2);
    }

    private void Add(Widget widget, int row)
    {
        Widgets.Add(widget);
        Grid.SetRow(widget, row);
    }

    private Widget CreatePortrait(MerchantDef merchant)
    {
        var tint = PortraitColor(merchant.Kind);
        IImage? portrait = null;
        if (!string.IsNullOrWhiteSpace(merchant.TexturePath)
            && Core.Content.TryLoad<Texture2D>(merchant.TexturePath, out var texture)
            && texture != null)
        {
            portrait = new TextureRegion(texture);
        }

        var frame = new Panel
        {
            Width = 180,
            Height = 220,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright],
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        if (portrait != null)
        {
            frame.Widgets.Add(new Image
            {
                Background = portrait,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            });
            return frame;
        }

        frame.Widgets.Add(new Panel
        {
            Background = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64],
                tint),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(12),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = merchant.Label.Length > 0 ? merchant.Label[0].ToString() : "?",
                    TextColor = Color.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        });
        return frame;
    }

    private static Color PortraitColor(MerchantKind kind) => kind switch
    {
        MerchantKind.GeneralStore => new Color(168, 132, 72),
        MerchantKind.Blacksmith => new Color(120, 88, 64),
        MerchantKind.Magician => new Color(92, 64, 140),
        MerchantKind.Alchemist => new Color(64, 112, 72),
        MerchantKind.Ranger => new Color(56, 96, 64),
        _ => new Color(80, 80, 88)
    };

    private Widget CreateShelf(RolledShelf shelf)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        foreach (var offer in shelf.Offers)
        {
            row.Widgets.Add(CreateOfferCard(offer));
        }

        return new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(10, 8),
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = shelf.Category.Label(),
                    TextColor = Color.Goldenrod
                },
                new ScrollViewer
                {
                    Content = row,
                    ShowHorizontalScrollBar = true,
                    ShowVerticalScrollBar = false,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                }
            }
        };
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

        var widgets = new List<Widget>
        {
            new Label(BaseContent.Styles.Label.Small)
            {
                Text = offer.DisplayLabel,
                TextColor = Color.Goldenrod,
                HorizontalAlignment = HorizontalAlignment.Center,
                Wrap = true,
                Width = 130
            }
        };

        if (offer.IsSet)
        {
            widgets.Add(CreateSetIcons(offer));
            widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "20% set",
                TextColor = Color.LightGreen,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        else if (offer.ItemDef != null)
        {
            widgets.Add(new Image
            {
                Background = offer.ItemDef.GetIconImage(),
                Width = 64,
                Height = 64,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        widgets.Add(buy);

        var card = new VerticalStackPanel
        {
            Width = 150,
            Spacing = 4,
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };
        foreach (var widget in widgets)
        {
            card.Widgets.Add(widget);
        }

        return card;
    }

    private static Widget CreateSetIcons(MerchantOffer offer)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        foreach (var piece in offer.SetPieces.Take(6))
        {
            row.Widgets.Add(new Image
            {
                Background = piece.GetIconImage(),
                Width = 20,
                Height = 20
            });
        }

        if (offer.SetPieces.Count > 6)
        {
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"+{offer.SetPieces.Count - 6}",
                TextColor = Color.Gray,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return row;
    }

    private Widget CreateSellCard(Item item)
    {
        var payout = ShopCatalog.GetSellPrice(item.ItemDef, _context.ArenaRun?.CurrentMerchant);
        var sell = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{payout}g",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        sell.Click += (_, _) => TrySell(item);

        return new VerticalStackPanel
        {
            Width = 110,
            Spacing = 3,
            Padding = new Thickness(6),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = item.LabelWithStackSize,
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Wrap = true,
                    Width = 98
                },
                new Image
                {
                    Background = item.GetIconImage(),
                    Width = 40,
                    Height = 40,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                sell
            }
        };
    }

    private void RebuildPack()
    {
        _packRow.Widgets.Clear();
        var items = _context.PlayerPawn.Inventory.ToList();
        if (items.Count == 0)
        {
            _packRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Empty",
                TextColor = Color.Gray,
                VerticalAlignment = VerticalAlignment.Center
            });
            return;
        }

        foreach (var item in items)
        {
            _packRow.Widgets.Add(CreateSellCard(item));
        }
    }

    private void TryBuy(MerchantOffer offer)
    {
        if (_context.ArenaRun!.TryBuy(_context, offer))
        {
            _status.TextColor = Color.LightGreen;
            _status.Text = $"Bought {offer.DisplayLabel}";
            _purse.Refresh();
            RebuildPack();
            return;
        }

        _status.TextColor = Color.IndianRed;
        _status.Text = $"Cannot buy {offer.DisplayLabel} ({offer.GoldCost}g, you have {_context.ArenaRun.Gold}g)";
    }

    private void TrySell(Item item)
    {
        var label = item.Label;
        if (_context.ArenaRun!.TrySell(_context, item))
        {
            _status.TextColor = Color.LightGreen;
            _status.Text = $"Sold {label}";
            _purse.Refresh();
            RebuildPack();
            return;
        }

        _status.TextColor = Color.IndianRed;
        _status.Text = $"Cannot sell {label}";
    }

    private void RefreshRunStats()
    {
        var run = _context.ArenaRun;
        if (run == null)
        {
            return;
        }

        _runStats.Text = $"Wins {run.Wins}/{ArenaRun.WinsToFinish}   Lives {run.LivesRemaining}";
    }

    public void Update()
    {
        _purse.Refresh();
        RefreshRunStats();
    }
}
