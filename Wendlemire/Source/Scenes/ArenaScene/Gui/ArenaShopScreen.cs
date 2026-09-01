using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaShopScreen : Grid
{
    private const int PackWidth = 210;
    private const int PortraitSize = 180;
    private const int OfferIconSize = 48;
    private const int PackIconSize = 36;
    private const int SetIconSize = 16;
    private static readonly Color TitleColor = new(214, 208, 196);
    private static readonly Color BodyColor = new(168, 164, 156);
    private static readonly Color ShelfWood = new(58, 28, 20);
    private static readonly Color ShelfHighlight = new(96, 48, 32);

    private readonly BaseGui _gui;
    private readonly GameContext _context;
    private readonly GoldPurse _purse;
    private readonly Label _status;
    private readonly Label _runStats;
    private readonly VerticalStackPanel _packBody;
    private readonly ScrollViewer _catalog;
    private readonly Grid _bodyHost;
    private readonly Grid _shopBody;
    private readonly CursorButton _shopTab;
    private readonly CursorButton _loadoutTab;
    private readonly Action _onDone;
    private readonly MerchantDef _merchant;
    private readonly List<(CursorButton Button, Label Price, int Cost)> _buyButtons = [];
    private readonly List<(CursorButton Button, Label Price, int Cost)> _refreshButtons = [];
    private IReadOnlyList<RolledShelf> _stock = [];
    private PawnPreparationPanel? _loadoutPanel;

    public ArenaShopScreen(BaseGui gui, GameContext context, Action onDone)
    {
        _gui = gui;
        _context = context;
        _onDone = onDone;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Padding = new Thickness(16, 12);
        ColumnSpacing = 0;
        RowSpacing = 10;
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Fill));

        var run = context.ArenaRun ?? throw new InvalidOperationException("Shop requires an ArenaRun.");
        _merchant = run.CurrentMerchant
                    ?? DefRepository<MerchantDef>.GetByMoniker("GeneralStore")
                    ?? throw new InvalidOperationException("No merchant selected.");

        _purse = new GoldPurse(context);
        _status = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Buy from the shelves, or sell worn gear and pack items at 1/3 value.",
            HorizontalAlignment = HorizontalAlignment.Left,
            TextColor = Color.Gray,
            Wrap = true
        };
        _runStats = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = BodyColor,
            VerticalAlignment = VerticalAlignment.Center
        };
        RefreshRunStats();
        _shopTab = CreateViewTab("Shop", ShowShop);
        _loadoutTab = CreateViewTab("Loadout", ShowLoadout);
        _shopTab.Enabled = false;

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
                    TextColor = TitleColor
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = _merchant.Description,
                    TextColor = BodyColor,
                    Wrap = true
                },
                new HorizontalStackPanel
                {
                    Spacing = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets = { _shopTab, _loadoutTab, _runStats }
                },
                _status
            }
        };
        header.Widgets.Add(info);
        Grid.SetColumn(info, 1);

        _catalog = new TooltipAwareScrollViewer
        {
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _stock = run.OpenShopVisit(
            _merchant,
            ShopStock.Roll(
                _merchant,
                run.RunSeed,
                run.FightsPlayed,
                ShopStock.OwnedUniqueMonikers(_context.Player)));
        RebuildCatalog();

        _packBody = new VerticalStackPanel
        {
            Spacing = 6,
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
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        done.Click += (_, _) => onDone();

        var packTitle = new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = "Your pack",
                    TextColor = TitleColor
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Sell worn or packed items at 1/3 value",
                    TextColor = BodyColor,
                    Wrap = true
                }
            }
        };
        _purse.HorizontalAlignment = HorizontalAlignment.Stretch;

        var packScroll = new TooltipAwareScrollViewer
        {
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = _packBody
        };

        var packPanel = new Grid
        {
            Width = PackWidth,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            RowSpacing = 8
        };
        packPanel.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        packPanel.RowsProportions.Add(new Proportion(ProportionType.Auto));
        packPanel.RowsProportions.Add(new Proportion(ProportionType.Auto));
        packPanel.RowsProportions.Add(new Proportion(ProportionType.Fill));
        packPanel.RowsProportions.Add(new Proportion(ProportionType.Auto));
        packPanel.Widgets.Add(packTitle);
        packPanel.Widgets.Add(_purse);
        Grid.SetRow(_purse, 1);
        packPanel.Widgets.Add(packScroll);
        Grid.SetRow(packScroll, 2);
        packPanel.Widgets.Add(done);
        Grid.SetRow(done, 3);

        _shopBody = new Grid
        {
            ColumnSpacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _shopBody.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _shopBody.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        _shopBody.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _shopBody.Widgets.Add(_catalog);
        _shopBody.Widgets.Add(packPanel);
        Grid.SetColumn(packPanel, 1);

        _bodyHost = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _bodyHost.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _bodyHost.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _bodyHost.Widgets.Add(_shopBody);

        Add(header, 0);
        Add(_bodyHost, 1);
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
            Width = PortraitSize,
            Height = PortraitSize,
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
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(8)
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
            ColumnSpacing = 8,
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
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                CreateShelfHeader(shelf),
                CreateSlotGrid(cards, shelf.Columns, shelf.ItemColumns)
            }
        };
    }

    private Widget CreateShelfHeader(RolledShelf shelf)
    {
        var header = new Grid
        {
            ColumnSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        header.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        header.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var title = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = shelf.Category.Label(),
            TextColor = TitleColor,
            VerticalAlignment = VerticalAlignment.Center
        };
        var refresh = CreateRefreshButton(shelf);
        header.Widgets.Add(title);
        header.Widgets.Add(refresh);
        Grid.SetColumn(refresh, 1);
        return header;
    }

    private Widget CreateRefreshButton(RolledShelf shelf)
    {
        var cost = ShopCatalog.ShelfRefreshCost(shelf.Category, shelf.RefreshCount);
        var price = new Label(BaseContent.Styles.Label.Small)
        {
            Text = cost == 0 ? "Reroll free" : $"Reroll {cost}g",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var refresh = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = price,
            VerticalAlignment = VerticalAlignment.Center
        };
        refresh.Click += (_, _) => TryRefreshShelf(shelf.Category);
        _refreshButtons.Add((refresh, price, cost));
        ApplyAffordability(refresh, price, cost);
        return refresh;
    }

    private static Widget CreateSlotGrid(IReadOnlyList<Widget> cards, int columns, int itemColumns)
    {
        var gridColumns = ShopLayout.NormalizeColumns(columns);
        var span = ShopLayout.NormalizeItemColumns(itemColumns, gridColumns);
        var slotsPerRow = ShopLayout.SlotsPerRow(gridColumns, span);
        var rows = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var rowCount = Math.Max(1, (int)Math.Ceiling(cards.Count / (float)slotsPerRow));
        for (var r = 0; r < rowCount; r++)
        {
            var row = new Grid
            {
                ColumnSpacing = 6,
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
            rows.Widgets.Add(CreateShelfLine());
        }

        return rows;
    }

    private static Widget CreateShelfLine()
    {
        return new Panel
        {
            Height = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidBrush(ShelfWood),
            Border = new SolidBrush(ShelfHighlight),
            BorderThickness = new Thickness(0, 2, 0, 1),
            Margin = new Thickness(2, 0, 2, 6)
        };
    }

    private Widget CreateOfferCard(MerchantOffer offer)
    {
        var title = new Label(BaseContent.Styles.Label.Small)
        {
            Text = offer.DisplayLabel,
            TextColor = TitleColor,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var body = new VerticalStackPanel
        {
            Spacing = 2,
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
            body.Widgets.Add(CreateOfferIcon(offer));
        }

        var buy = CreateBuyButton(offer);
        var card = new Grid
        {
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SmallFrame]
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
        var buy = new CursorButton(BaseContent.Styles.Button.Small)
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

    private static Widget CreateOfferIcon(MerchantOffer offer)
    {
        var icon = new Image
        {
            Background = offer.ItemDef!.GetIconImage(),
            Width = OfferIconSize,
            Height = OfferIconSize,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        if (offer.Available <= 1)
        {
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.Margin = new Thickness(6);
            return icon;
        }

        return new HorizontalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                icon,
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"x{offer.Available}",
                    TextColor = TitleColor,
                    VerticalAlignment = VerticalAlignment.Bottom
                }
            }
        };
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
                Width = SetIconSize,
                Height = SetIconSize
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

    private Widget CreateSellCard(Item item, bool equipped)
    {
        var payout = ShopCatalog.GetSellPrice(item.ItemDef);
        var sell = new CursorButton(BaseContent.Styles.Button.Small)
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

        var card = new VerticalStackPanel
        {
            Spacing = 2,
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SmallFrame]
        };
        card.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = item.LabelWithStackSize,
            TextColor = TitleColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true
        });
        if (equipped)
        {
            card.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "worn",
                TextColor = Color.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        card.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
            Width = PackIconSize,
            Height = PackIconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(6)
        });
        card.Widgets.Add(sell);
        return card.WithTooltip(() => CreateEntityInspect(item));
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
        _refreshButtons.Clear();
        var shelves = new VerticalStackPanel
        {
            Spacing = 22,
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
                ItemColumns = shelf.ItemColumns,
                RefreshCount = shelf.RefreshCount
            })
            .ToList();
        RebuildCatalog();
    }

    private void RebuildPack()
    {
        _packBody.Widgets.Clear();
        var items = ShopPack.SellableItems(_context.PlayerPawn).ToList();
        if (items.Count == 0)
        {
            _packBody.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Nothing to sell",
                TextColor = Color.Gray,
                Wrap = true
            });
            return;
        }

        foreach (var (item, equipped) in items)
        {
            _packBody.Widgets.Add(CreateSellCard(item, equipped));
        }
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

    private void TryRefreshShelf(ShopCategory category)
    {
        var run = _context.ArenaRun!;
        var cost = ShopCatalog.ShelfRefreshCost(
            category,
            run.ShopShelves.FirstOrDefault(shelf => shelf.Category == category)?.RefreshCount ?? 0);
        var label = category.Label();
        if (run.TryRefreshShelf(_merchant, category, ShopStock.OwnedUniqueMonikers(_context.Player)))
        {
            _status.TextColor = Color.LightGreen;
            _status.Text = $"Refreshed {label}";
            _purse.Refresh();
            _stock = ShopStock.Restore(_merchant, run.ShopShelves);
            RebuildCatalog();
            RebuildPack();
            return;
        }

        _status.TextColor = Color.IndianRed;
        _status.Text = $"Cannot refresh {label} ({cost}g, you have {run.Gold}g)";
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

        foreach (var (button, price, cost) in _refreshButtons)
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

    private CursorButton CreateViewTab(string label, Action onClick)
    {
        var tab = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = label,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        tab.Click += (_, _) => onClick();
        return tab;
    }

    private void ShowShop()
    {
        SetView(shop: true);
        RebuildPack();
    }

    private void ShowLoadout()
    {
        _loadoutPanel?.RemoveFromParent();
        _loadoutPanel = new PawnPreparationPanel(_gui, _context.PlayerPawn, showGrimoire: false);
        var done = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label
            {
                Text = "Continue",
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        done.Click += (_, _) => _onDone();
        _loadoutPanel.SetControls(done);
        SetView(shop: false);
    }

    private void SetView(bool shop)
    {
        _shopTab.Enabled = !shop;
        _loadoutTab.Enabled = shop;
        _shopBody.RemoveFromParent();
        _loadoutPanel?.RemoveFromParent();
        _bodyHost.Widgets.Clear();
        Widget? body = shop ? _shopBody : _loadoutPanel;
        if (body == null)
        {
            return;
        }

        body.HorizontalAlignment = HorizontalAlignment.Stretch;
        body.VerticalAlignment = VerticalAlignment.Stretch;
        _bodyHost.Widgets.Add(body);
    }

    public void Update()
    {
        _purse.Refresh();
        RefreshRunStats();
        RefreshAffordability();
        _loadoutPanel?.Update();
        TooltipHelper.UpdatePosition();
    }
}
