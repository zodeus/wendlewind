using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui.Widgets.TownWidgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets;

public class MerchantContainerPanel : VerticalStackPanel {
    private readonly EntityContainer _container;
    private readonly EntityContainer _receivingContainer;

    private readonly List<MerchantListPanel> _sections = new();
    private readonly Label _weightLabel;

    public MerchantContainerPanel(EntityContainer container, EntityContainer receivingContainer, string title, MerchantTransactionType merchantTransactionType) {
        _container = container;
        _receivingContainer = receivingContainer;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(30);
        Spacing = 10;

        List<ItemContainerPanelSection> sections = new() {
            new ItemContainerPanelSection {
                Label = "Consumables",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType is ItemType.Medical or ItemType.TradeTool || entity.Def == Defs.Items.Cauterize
            },
            new ItemContainerPanelSection {
                Label = "Potions",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType == ItemType.Potion
            },
            new ItemContainerPanelSection {
                Label = "Equipment",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType == ItemType.Equipment
            },
            new ItemContainerPanelSection {
                Label = "Resources",
                Container = _container,
                Filter = entity => ((Item) entity).ItemDef.ItemType == ItemType.Resource
            }
        };

        AddChild(new Label(BaseContent.Styles.Label.Large) { Text = title });
        Proportions.Add(Proportion.Auto);
        foreach (ItemContainerPanelSection section in sections) {
            Proportions.Add(Proportion.Auto);
            Proportions.Add(Proportion.Auto);
            Proportions.Add(Proportion.Auto);

            MerchantListPanel panel = new(section.Container, merchantTransactionType, section.Filter, TradeHandler) {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _sections.Add(panel);
            AddChild(new HorizontalSeparator());
            AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = section.Label });
            AddChild(new ScrollViewer { Content = panel, MaxHeight = 200});
        }

        Proportions.Add(Proportion.Fill);
        _weightLabel = new Label(BaseContent.Styles.Label.Large) {
            VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right
        };
        AddChild(_weightLabel);
    }

    private void TradeHandler(Item item, int amount, MerchantTransactionType transactionType) {
        if (amount > item.StackSize) {
            Log.Error("TradeHandler: amountWanted was larger than Item.StackSize");
            amount = item.StackSize;
        }

        int cost = item.GetCurrencyValue(transactionType) * amount;
        if (_receivingContainer.Contains(Defs.Items.Coin, cost) == false) {
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Red, Duration = 2, Text = "Not enough coin"
            });
            return;
        }

        if (_receivingContainer.HasCapacityFor(item, amount) == false) {
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Red, Duration = 2, Text = "Cannot purchase, exceeds container weight limit"
            });
            return;
        }

        _receivingContainer.TryAdd(item, amount);
        Item? coins = _receivingContainer.Take(Defs.Items.Coin, cost);
        if (coins == null) {
            Log.Error("coin value is null during trade");
        }

        _container.TryAdd(coins);

        string text = transactionType == MerchantTransactionType.Buy ? "Purchased" : "SOLD";
        Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
            Color = Color.Gold, Duration = 2, Text = text
        });
    }

    public void Update() {
        foreach (MerchantListPanel section in _sections) {
            section.Update();
        }

        _weightLabel.Text = $"{_container.Weight}/{_container.MaxWeight}";
    }

    private class ItemContainerPanelSection {
        public string Label { get; set; } = null!;
        public EntityContainer Container { get; set; } = null!;
        public Func<Entity, bool> Filter { get; set; } = null!;
    }
}

public class MerchantListPanelItem : HorizontalStackPanel {
    private readonly Item _item;
    private readonly Label _stackSizeLabel;

    public MerchantListPanelItem(Item item, MerchantTransactionType merchantTransactionType, Action<Item, int, MerchantTransactionType> tradeHandler) {
        Spacing = 10;
        _item = item;

        // ITEM LABEL
        HorizontalStackPanel entityButton = new() {
            Spacing = 10, Width = 400,
            Widgets = {
                new Image { Background = new TextureRegion(item.Icon), Width = 32, Height = 32 },
                new Label { Text = _item.Label, VerticalAlignment = VerticalAlignment.Center }
            }
        };
        AddChild(entityButton);
        Proportions.Add(Proportion.Auto);

        // STACK SIZE LABEL
        _stackSizeLabel = new Label {
            Font = BaseContent.Fonts.Fancy.Normal,
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
        };
        AddChild(new Panel {
            Width = 55, Height = 32, Widgets = { _stackSizeLabel },
            VerticalAlignment = VerticalAlignment.Center,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        });
        Proportions.Add(Proportion.Auto);

        if (_item.Def == Defs.Items.Coin || _item.Def == Defs.Items.Cauterize) {
            return;
        }

        // CURRENCY VALUE TEXT BOX
        AddChild(new HorizontalStackPanel {
            Padding = new Thickness(5, 3, 5, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = {
                new Label {
                    Text = _item.GetCurrencyValue(merchantTransactionType).ToString(CultureInfo.InvariantCulture),
                    TextAlign = TextAlign.Right,
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Coin], Width = 24, Height = 24
                }
            },
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        });
        Proportions.Add(Proportion.Auto);

        // TRADE AMOUNT CONTROLS
        Label tradeValueTotalLabel = new() {
            TextAlign = TextAlign.Right,
            Width = 40,
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox amountTextBox = new() {
            Width = 20, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        amountTextBox.ValueChanging += (_, args) => {
            int oldValue = int.Parse(args.OldValue == null ? "0" : args.OldValue);
            int newValue = int.Parse(args.NewValue);
            if (oldValue == newValue + 1 || oldValue == newValue - 1) {
                if (newValue < 0 || newValue > _item.StackSize) {
                    args.Cancel = true;
                }
            }
            else {
                args.Cancel = true;
            }
        };
        amountTextBox.TextChanged += (_, args) => {
            int value = int.Parse(args.NewValue);
            tradeValueTotalLabel.Text = (value * _item.GetCurrencyValue(merchantTransactionType)).ToString(CultureInfo.InvariantCulture);
        };
        amountTextBox.Text = "1";

        ImageButton minusButton = new(BaseContent.Styles.Button.Minus24) {
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right
        };
        minusButton.Click += (_, _) => amountTextBox.Text = (int.Parse(amountTextBox.Text) - 1).ToString();
        ImageButton plusButton = new(BaseContent.Styles.Button.Plus24) { VerticalAlignment = VerticalAlignment.Center };
        plusButton.Click += (_, _) => amountTextBox.Text = (int.Parse(amountTextBox.Text) + 1).ToString();
        AddChild(minusButton);
        Proportions.Add(Proportion.Auto);
        AddChild(amountTextBox);
        AddChild(plusButton);
        AddChild(new HorizontalStackPanel {
            Padding = new Thickness(5, 3, 5, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = {
                tradeValueTotalLabel,
                new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Coin], Width = 24, Height = 24
                }
            },
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame]
        });

        // TRADE BUTTON
        TextButton tradeButton = new(BaseContent.Styles.Button.Small) {
            VerticalAlignment = VerticalAlignment.Center,
            Text = merchantTransactionType == MerchantTransactionType.Buy ? "BUY" : "SELL"
        };
        tradeButton.Click += (_, _) => {
            int amount = int.Parse(amountTextBox.Text);
            if (amount < 1) {
                return;
            }

            tradeHandler.Invoke(item, amount, merchantTransactionType);
        };
        AddChild(tradeButton);
    }

    public void Update() {
        _stackSizeLabel.Text = $"{_item.StackSize}x";
    }
}

public class MerchantListPanel : VerticalStackPanel {
    private readonly EntityContainer _container;
    private readonly MerchantTransactionType _merchantTransactionType;
    private readonly Action<Item, int, MerchantTransactionType> _tradeHandler;
    private readonly Dictionary<Entity, MerchantListPanelItem> _items = new();

    private Func<Entity, bool>? Filter { get; }

    public MerchantListPanel(EntityContainer container, MerchantTransactionType merchantTransactionType, Func<Entity, bool> filter, Action<Item, int, MerchantTransactionType> tradeHandler) {
        Spacing = 5;
        _container = container;
        _merchantTransactionType = merchantTransactionType;
        _tradeHandler = tradeHandler;
        Filter = filter;
    }

    public void Update() {

        foreach (Item entity in _container) {
            if (Filter != null && Filter(entity) == false) {
                continue;
            }

            if (!_items.ContainsKey(entity)) {
                _items[entity] = new MerchantListPanelItem(entity, _merchantTransactionType, _tradeHandler);
                AddChild(_items[entity]);
            }
        }

        foreach ((Entity item, MerchantListPanelItem panel) in _items) {
            if (item.IsDestroyed || _container.Contains(item) == false) {
                panel.RemoveFromParent();
                _items.Remove(item);
                continue;
            }

            panel.Update();
        }
    }
}