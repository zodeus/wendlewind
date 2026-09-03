using FontStashSharp.RichText;
using Wendlemire.Scenes.MainGameScene.Gui;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaShopScreen : Grid
{
    private const int InventoryWidth = 280;
    private const int PortraitSize = 180;
    private const int OfferIconSize = 48;
    private const int InventoryIconSize = 32;
    private const int SetIconSize = 16;
    private const string EquippedGroup = "Equipped";
    private static readonly string[] InventoryGroupOrder =
    [
        "Weapons",
        "Armor",
        "Cloaks",
        "Bags",
        "Gear",
        "Potions",
        "Enchantments",
        "Food",
        "Ammo",
        "Medicine",
        "Incense",
        "Trinkets",
        "Resources",
        "Supplies",
        "Other"
    ];
    private static readonly Color TitleColor = new(214, 208, 196);
    private static readonly Color BodyColor = new(168, 164, 156);
    private static readonly Color ShelfWood = new(58, 28, 20);
    private static readonly Color ShelfHighlight = new(96, 48, 32);

    private readonly BaseGui _gui;
    private readonly GameContext _context;
    private readonly GoldPurse _purse;
    private readonly Label _status;
    private readonly Label _runStats;
    private readonly VerticalStackPanel _inventoryBody;
    private readonly ScrollViewer _catalog;
    private readonly Action _onSave;
    private readonly MerchantDef _merchant;
    private readonly List<(CursorButton Button, Label Price, int Cost)> _buyButtons = [];
    private readonly List<(CursorButton Button, Label Price, int Cost)> _refreshButtons = [];
    private IReadOnlyList<RolledShelf> _stock = [];

    public ArenaShopScreen(BaseGui gui, GameContext context, Action onDone, Action onSave)
    {
        _gui = gui;
        _context = context;
        _onSave = onSave;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Padding = new Thickness(16, 12);
        ColumnSpacing = 12;
        RowSpacing = 0;
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Fill));

        var run = context.ArenaRun ?? throw new InvalidOperationException("Shop requires an ArenaRun.");
        _merchant = run.CurrentMerchant
                    ?? DefRepository<MerchantDef>.GetByMoniker("GeneralStore")
                    ?? throw new InvalidOperationException("No merchant selected.");

        _purse = new GoldPurse(context);
        _status = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Buy from the shelves, or sell worn gear and inventory items at 1/3 value.",
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
        var loadout = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Loadout",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            VerticalAlignment = VerticalAlignment.Center
        };
        loadout.Click += (_, _) => onDone();

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
                    Widgets = { loadout, _runStats }
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

        _inventoryBody = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        RebuildInventory();

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

        var inventoryTitle = new VerticalStackPanel
        {
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Medium)
                {
                    Text = "Inventory",
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

        var inventoryScroll = new TooltipAwareScrollViewer
        {
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = _inventoryBody
        };

        var shopBody = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            RowSpacing = 10
        };
        shopBody.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        shopBody.RowsProportions.Add(new Proportion(ProportionType.Auto));
        shopBody.RowsProportions.Add(new Proportion(ProportionType.Fill));
        shopBody.Widgets.Add(header);
        shopBody.Widgets.Add(_catalog);
        Grid.SetRow(_catalog, 1);

        var inventoryPanel = new Grid
        {
            Width = InventoryWidth,
            Padding = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            RowSpacing = 8
        };
        inventoryPanel.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        inventoryPanel.RowsProportions.Add(new Proportion(ProportionType.Auto));
        inventoryPanel.RowsProportions.Add(new Proportion(ProportionType.Auto));
        inventoryPanel.RowsProportions.Add(new Proportion(ProportionType.Fill));
        inventoryPanel.RowsProportions.Add(new Proportion(ProportionType.Auto));
        inventoryPanel.Widgets.Add(inventoryTitle);
        inventoryPanel.Widgets.Add(_purse);
        Grid.SetRow(_purse, 1);
        inventoryPanel.Widgets.Add(inventoryScroll);
        Grid.SetRow(inventoryScroll, 2);
        inventoryPanel.Widgets.Add(done);
        Grid.SetRow(done, 3);

        Widgets.Add(shopBody);
        Widgets.Add(inventoryPanel);
        Grid.SetColumn(inventoryPanel, 1);
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
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextAlign = TextHorizontalAlignment.Center,
            SingleLine = true,
            AutoEllipsisMethod = AutoEllipsisMethod.Word
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
            ClipToBounds = true,
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

    private Widget CreateSellRow(Item item)
    {
        var payout = ShopCatalog.GetSellPrice(item.ItemDef);
        var icon = new Panel
        {
            Width = InventoryIconSize + 6,
            Height = InventoryIconSize + 6,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                new Image
                {
                    Background = item.GetIconImage(),
                    Width = InventoryIconSize,
                    Height = InventoryIconSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
        var name = new Label(BaseContent.Styles.Label.Small)
        {
            Text = item.LabelWithStackSize,
            TextColor = TitleColor,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SingleLine = true,
            AutoEllipsisMethod = AutoEllipsisMethod.Word
        };
        var price = new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{payout}g",
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };

        var body = new Grid
        {
            ColumnSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        body.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        body.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        body.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        body.Widgets.Add(icon);
        body.Widgets.Add(name);
        Grid.SetColumn(name, 1);
        body.Widgets.Add(price);
        Grid.SetColumn(price, 2);

        var row = new CursorButton(BaseContent.Styles.Button.Dark)
        {
            Content = body,
            Padding = new Thickness(6, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        row.Click += (_, _) => TrySell(item);
        return row.WithTooltip(() => CreateEntityInspect(item));
    }

    private Widget CreateInventoryGroup(string title, IReadOnlyList<Item> items, bool equipped)
    {
        var group = new VerticalStackPanel
        {
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = title,
                    TextColor = equipped ? Color.Goldenrod : TitleColor
                }
            }
        };
        foreach (var item in items)
        {
            group.Widgets.Add(CreateSellRow(item));
        }

        return group;
    }

    private static string InventoryGroup(Item item)
    {
        var def = item.ItemDef;
        if (def.AmmoProperties != null && def.ItemType is ItemType.Supplies or ItemType.Resource)
        {
            return "Ammo";
        }

        return def.ItemType switch
        {
            ItemType.Food => "Food",
            ItemType.Potion => "Potions",
            ItemType.Medical => "Medicine",
            ItemType.Incense => "Incense",
            ItemType.Trinket => "Trinkets",
            ItemType.Enchantment => "Enchantments",
            ItemType.Equipment => def.EquipmentProperties?.EquipmentType switch
            {
                EquipmentType.Weapon => "Weapons",
                EquipmentType.Armor when def.EquipmentProperties.SlotUsedToEquip == EquipmentSlotType.Cloak => "Cloaks",
                EquipmentType.Armor => "Armor",
                EquipmentType.Bag => "Bags",
                _ => "Gear"
            },
            ItemType.Resource => "Resources",
            ItemType.Supplies => "Supplies",
            _ => "Other"
        };
    }

    private static int InventoryGroupSort(string group)
    {
        var index = Array.IndexOf(InventoryGroupOrder, group);
        return index < 0 ? InventoryGroupOrder.Length : index;
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
        var discountPct = (int)Math.Round((1f - ShopCatalog.SetDiscount) * 100f);
        var header = new Grid { ColumnSpacing = 8 };
        header.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        header.Widgets.Add(new Label("small")
        {
            Text = offer.DisplayLabel,
            TextColor = Color.Goldenrod
        });
        var price = new Label("small")
        {
            Text = $"{discountPct}% · {offer.ResolveGoldCost()}g",
            TextColor = Color.LightGreen
        };
        header.Widgets.Add(price);
        Grid.SetColumn(price, 1);

        var list = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 1
        };
        list.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        list.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        list.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        list.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var row = 0;
        foreach (var group in offer.SetPieces.GroupBy(def => def.Moniker))
        {
            var piece = group.First();
            var count = group.Count();

            var icon = new Image
            {
                Background = piece.GetIconImage(),
                Width = 24,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
            list.Widgets.Add(icon);
            Grid.SetRow(icon, row);

            var name = new Label("small")
            {
                Text = count > 1 ? $"{piece.Label} x{count}" : piece.Label,
                TextColor = TitleColor,
                VerticalAlignment = VerticalAlignment.Center
            };
            list.Widgets.Add(name);
            Grid.SetColumn(name, 1);
            Grid.SetRow(name, row);

            var slot = new Label("small")
            {
                Text = FormatEquipSlot(piece.EquipmentProperties?.SlotUsedToEquip),
                TextColor = new Color(100, 180, 255),
                VerticalAlignment = VerticalAlignment.Center
            };
            list.Widgets.Add(slot);
            Grid.SetColumn(slot, 2);
            Grid.SetRow(slot, row);

            var stats = new Label("small")
            {
                Text = FormatSetPieceStats(piece),
                TextColor = Color.LightGoldenrodYellow,
                VerticalAlignment = VerticalAlignment.Center
            };
            list.Widgets.Add(stats);
            Grid.SetColumn(stats, 3);
            Grid.SetRow(stats, row);

            row++;
        }

        var body = new VerticalStackPanel
        {
            Spacing = 4,
            Widgets = { header, list }
        };

        return new ScrollViewer
        {
            Content = body,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            MaxHeight = 380
        };
    }

    private static string FormatEquipSlot(EquipmentSlotType? slot)
    {
        if (slot == null || slot == EquipmentSlotType.Invalid)
        {
            return "";
        }

        var name = slot.Value.ToString();
        return name.EndsWith("Armor", StringComparison.Ordinal)
            ? name[..^5]
            : name;
    }

    private static string FormatSetPieceStats(ItemDef piece)
    {
        var parts = piece.BaseStats
            .Where(stat => stat.Def != Defs.Stats.MaxDurability)
            .Select(stat =>
            {
                var value = stat.Value % 1 == 0 ? $"{stat.Value:0}" : $"{stat.Value:0.##}";
                return $"{AbbreviateStat(stat.Def.Label)} {value}";
            });
        return string.Join("  ", parts);
    }

    private static string AbbreviateStat(string label) => label switch
    {
        "Physical Resistance" => "Phys",
        "Move Speed" => "Move",
        _ => label
    };

    private Widget CreateItemInspect(ItemDef def) =>
        CreateEntityInspect(_context.Factory.CreateEntity<Item>(def, 1));

    private Widget CreateEntityInspect(Item item)
    {
        var properties = new EntityPanelProperties
        {
            ShowTitle = false,
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

    private void RebuildInventory()
    {
        _inventoryBody.Widgets.Clear();
        var items = ShopPack.SellableItems(_context.PlayerPawn).ToList();
        if (items.Count == 0)
        {
            _inventoryBody.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Nothing to sell",
                TextColor = Color.Gray,
                Wrap = true
            });
            return;
        }

        foreach (var group in items
                     .Where(entry => !entry.Equipped)
                     .GroupBy(entry => InventoryGroup(entry.Item))
                     .OrderBy(group => InventoryGroupSort(group.Key)))
        {
            _inventoryBody.Widgets.Add(CreateInventoryGroup(group.Key, group.Select(entry => entry.Item).ToList(), equipped: false));
        }

        var equipped = items.Where(entry => entry.Equipped).Select(entry => entry.Item).ToList();
        if (equipped.Count > 0)
        {
            _inventoryBody.Widgets.Add(CreateInventoryGroup(EquippedGroup, equipped, equipped: true));
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

            RebuildInventory();
            _onSave();
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
            RebuildInventory();
            _onSave();
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
            RebuildInventory();
            RefreshAffordability();
            _onSave();
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

    public void Update()
    {
        _purse.Refresh();
        RefreshRunStats();
        RefreshAffordability();
        TooltipHelper.UpdatePosition();
    }
}
