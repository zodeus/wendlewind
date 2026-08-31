using Wendlewind.Scenes.MainGameScene.Gui;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaShopScreen : Grid
{
    private const int PackItemColumns = 1;

    private readonly BaseGui _gui;
    private readonly GameContext _context;
    private readonly GoldPurse _purse;
    private readonly Label _status;
    private readonly Label _runStats;
    private readonly VerticalStackPanel _packBody;
    private readonly ScrollViewer _catalog;
    private readonly MerchantDef _merchant;
    private readonly List<(CursorButton Button, Label Price, int Cost)> _buyButtons = [];
    private IReadOnlyList<RolledShelf> _stock = [];

    public ArenaShopScreen(BaseGui gui, GameContext context, Action onDone)
    {
        _gui = gui;
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
        _merchant = run.CurrentMerchant
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
        header.Widgets.Add(CreatePortrait(_merchant));
        var info = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Huge)
                {
                    Text = _merchant.Label,
                    TextColor = Color.Goldenrod
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = _merchant.Description,
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

        _catalog = new ScrollViewer
        {
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _stock = ShopStock.Roll(
            _merchant,
            run.RunSeed,
            run.FightsPlayed,
            ShopStock.OwnedUniqueMonikers(_context.Player));
        RebuildCatalog();

        _packBody = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        RebuildPack();

        var done = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label
            {
                Text = "Continue",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 180
        };
        done.Click += (_, _) => onDone();

        var packHeader = new Grid
        {
            ColumnSpacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        packHeader.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        packHeader.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        var packTitle = new VerticalStackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = "Your pack",
                    TextColor = Color.Goldenrod
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Sell for one-tenth value",
                    TextColor = Color.Gray
                }
            }
        };
        packHeader.Widgets.Add(packTitle);
        packHeader.Widgets.Add(done);
        Grid.SetColumn(done, 1);

        var footer = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = { packHeader, _packBody }
        };

        Add(header, 0);
        Add(_catalog, 1);
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

    private Widget CreateShelfRow(IReadOnlyList<RolledShelf> shelves)
    {
        if (shelves.Count == 1 && shelves[0].Columns >= ShopLayout.GridColumns)
        {
            return CreateShelf(shelves[0]);
        }

        var row = new Grid
        {
            ColumnSpacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        for (var i = 0; i < ShopLayout.GridColumns; i++)
        {
            row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        }

        var column = 0;
        foreach (var shelf in shelves)
        {
            var widget = CreateShelf(shelf);
            row.Widgets.Add(widget);
            Grid.SetColumn(widget, column);
            Grid.SetColumnSpan(widget, shelf.Columns);
            column += shelf.Columns;
        }

        return row;
    }

    private Widget CreateShelf(RolledShelf shelf)
    {
        var cards = shelf.Offers.Select(CreateOfferCard).ToList();
        return new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = shelf.Category.Label(),
                    TextColor = Color.Goldenrod
                },
                CreateSlotGrid(cards, shelf.Columns, shelf.ItemColumns),
                CreateShelfLine()
            }
        };
    }

    private static Widget CreateSlotGrid(IReadOnlyList<Widget> cards, int columns, int itemColumns)
    {
        var gridColumns = ShopLayout.NormalizeColumns(columns);
        var span = ShopLayout.NormalizeItemColumns(itemColumns, gridColumns);
        var slotsPerRow = ShopLayout.SlotsPerRow(gridColumns, span);
        var rows = new VerticalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var rowCount = Math.Max(1, (int)Math.Ceiling(cards.Count / (float)slotsPerRow));
        for (var r = 0; r < rowCount; r++)
        {
            var row = new Grid
            {
                ColumnSpacing = 10,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            for (var c = 0; c < gridColumns; c++)
            {
                row.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
            }

            row.RowsProportions.Add(new Proportion(ProportionType.Auto));

            for (var c = 0; c < slotsPerRow; c++)
            {
                var index = r * slotsPerRow + c;
                var cell = new Panel
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                if (index < cards.Count)
                {
                    var card = cards[index];
                    card.HorizontalAlignment = HorizontalAlignment.Stretch;
                    card.VerticalAlignment = VerticalAlignment.Stretch;
                    cell.Widgets.Add(card);
                }

                row.Widgets.Add(cell);
                Grid.SetColumn(cell, c * span);
                Grid.SetColumnSpan(cell, span);
            }

            rows.Widgets.Add(row);
        }

        return rows;
    }

    private static Widget CreateShelfLine()
    {
        return new Panel
        {
            Height = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidBrush(new Color(196, 156, 72)),
            Margin = new Thickness(2, 0, 2, 4)
        };
    }

    private Widget CreateOfferCard(MerchantOffer offer)
    {
        var title = new Label(BaseContent.Styles.Label.Small)
        {
            Text = offer.Available > 1 ? $"{offer.DisplayLabel}  x{offer.Available}" : offer.DisplayLabel,
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var body = new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (offer.IsSet)
        {
            body.Widgets.Add(CreateSetIcons(offer));
            body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "20% set",
                TextColor = Color.LightGreen,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        else if (offer.ItemDef != null)
        {
            body.Widgets.Add(new Image
            {
                Background = offer.ItemDef.GetIconImage(),
                Width = 64,
                Height = 64,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        var buy = CreateBuyButton(offer);
        var card = new Grid
        {
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright]
        };
        card.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        card.RowsProportions.Add(new Proportion(ProportionType.Auto));
        card.RowsProportions.Add(new Proportion(ProportionType.Fill));
        card.RowsProportions.Add(new Proportion(ProportionType.Auto));
        card.Widgets.Add(title);
        card.Widgets.Add(body);
        card.Widgets.Add(buy);
        Grid.SetRow(body, 1);
        Grid.SetRow(buy, 2);
        return card.WithTooltip(() => CreateOfferInspect(offer));
    }

    private Widget CreateBuyButton(MerchantOffer offer)
    {
        var cost = offer.ResolveGoldCost();
        var price = new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{cost}g",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var buy = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = price,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0
        };
        buy.Click += (_, _) => TryBuy(offer);
        buy.WithTooltip(() => CreateOfferInspect(offer));
        _buyButtons.Add((buy, price, cost));
        ApplyAffordability(buy, price, cost);
        return buy;
    }

    private Widget CreateSetIcons(MerchantOffer offer)
    {
        const int maxSlots = 4;
        var pieces = offer.SetPieces;
        var overflow = pieces.Count > maxSlots ? pieces.Count - (maxSlots - 1) : 0;
        var iconCount = overflow > 0 ? maxSlots - 1 : pieces.Count;
        var columns = iconCount + (overflow > 0 ? 1 : 0);

        var row = new Grid
        {
            ColumnSpacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        for (var i = 0; i < columns; i++)
        {
            row.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        }

        for (var i = 0; i < iconCount; i++)
        {
            var piece = pieces[i];
            var icon = new Image
            {
                Background = piece.GetIconImage(),
                Width = 20,
                Height = 20
            }.WithTooltip(() => CreateItemInspect(piece));
            row.Widgets.Add(icon);
            Grid.SetColumn(icon, i);
        }

        if (overflow > 0)
        {
            var extra = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"+{overflow}",
                TextColor = Color.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            row.Widgets.Add(extra);
            Grid.SetColumn(extra, iconCount);
        }

        return row;
    }

    private Widget CreateSellCard(Item item)
    {
        var payout = ShopCatalog.GetSellPrice(item.ItemDef);
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
        sell.WithTooltip(() => CreateEntityInspect(item));

        return new VerticalStackPanel
        {
            Spacing = 4,
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright],
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = item.LabelWithStackSize,
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new Image
                {
                    Background = item.GetIconImage(),
                    Width = 48,
                    Height = 48,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                sell
            }
        }.WithTooltip(() => CreateEntityInspect(item));
    }

    private Widget CreateOfferInspect(MerchantOffer offer)
    {
        if (offer.IsSet)
        {
            return CreateSetInspect(offer);
        }

        return CreateItemInspect(offer.ItemDef!);
    }

    private Widget CreateSetInspect(MerchantOffer offer)
    {
        var body = new VerticalStackPanel
        {
            Spacing = 10
        };
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = offer.DisplayLabel,
            TextColor = Color.Goldenrod
        });
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"20% set · {offer.ResolveGoldCost()}g",
            TextColor = Color.LightGreen
        });
        foreach (var piece in offer.SetPieces.DistinctBy(def => def.Moniker))
        {
            body.Widgets.Add(CreateItemInspect(piece));
        }

        return new ScrollViewer
        {
            Content = body,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            MaxHeight = 520
        };
    }

    private Widget CreateItemInspect(ItemDef def) =>
        CreateEntityInspect(_context.Factory.CreateEntity<Item>(def, 1));

    private Widget CreateEntityInspect(Item item)
    {
        var properties = new EntityPanelProperties
        {
            ShowTitle = true,
            ShowCloseButton = false,
            Background = null
        };

        if (item.ItemDef.ItemType == ItemType.Trinket)
        {
            return new TrinketPanel(_gui, item, properties);
        }

        return EntityPanelFactory.Create(_gui, item, properties);
    }

    private void RebuildCatalog()
    {
        _buyButtons.Clear();
        var shelves = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        foreach (var row in ShopLayout.GroupRows(_stock, shelf => shelf.Columns))
        {
            shelves.Widgets.Add(CreateShelfRow(row));
        }

        _catalog.Content = shelves;
    }

    private void RemovePurchasedOffer(MerchantOffer offer)
    {
        _stock = _stock
            .Select(shelf => new RolledShelf
            {
                Category = shelf.Category,
                Offers = shelf.Offers.Where(o => o != offer).ToList(),
                Columns = shelf.Columns,
                ItemColumns = shelf.ItemColumns
            })
            .ToList();
        RebuildCatalog();
    }

    private void RebuildPack()
    {
        _packBody.Widgets.Clear();
        var items = _context.PlayerPawn.Inventory.ToList();
        if (items.Count == 0)
        {
            _packBody.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Nothing to sell",
                TextColor = Color.Gray
            });
            _packBody.Widgets.Add(CreateShelfLine());
            return;
        }

        _packBody.Widgets.Add(CreateSlotGrid(
            items.Select(CreateSellCard).ToList(),
            ShopLayout.GridColumns,
            PackItemColumns));
        _packBody.Widgets.Add(CreateShelfLine());
    }

    private void TryBuy(MerchantOffer offer)
    {
        var cost = offer.ResolveGoldCost();
        var label = offer.DisplayLabel;
        if (_context.ArenaRun!.TryBuy(_context, offer))
        {
            _status.TextColor = Color.LightGreen;
            _status.Text = $"Bought {label}";
            _purse.Refresh();
            offer.Available--;
            if (offer.Available <= 0)
            {
                RemovePurchasedOffer(offer);
            }
            else
            {
                RebuildCatalog();
            }

            RebuildPack();
            return;
        }

        _status.TextColor = Color.IndianRed;
        _status.Text = $"Cannot buy {label} ({cost}g, you have {_context.ArenaRun.Gold}g)";
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
            RefreshAffordability();
            return;
        }

        _status.TextColor = Color.IndianRed;
        _status.Text = $"Cannot sell {label}";
    }

    private void RefreshAffordability()
    {
        foreach (var (button, price, cost) in _buyButtons)
        {
            ApplyAffordability(button, price, cost);
        }
    }

    private void ApplyAffordability(CursorButton button, Label price, int cost)
    {
        var canAfford = (_context.ArenaRun?.Gold ?? 0) >= cost;
        button.Enabled = canAfford;
        price.TextColor = canAfford ? Color.White : new Color(140, 90, 90);
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
        RefreshAffordability();
        TooltipHelper.UpdatePosition();
    }
}
