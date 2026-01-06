namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.ArmorPanels;

[UsedImplicitly]
public sealed class RejuvenationCloakPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly RejuvenationCloakHandler _handler;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;
    private readonly VerticalStackPanel _upgradeSection;
    private readonly Label _bonusLabel;
    private readonly Label _upgradeLevelLabel;

    // Color palette
    private static readonly Color HealingColor = new(120, 220, 160);
    private static readonly Color GoldColor = Color.Gold;
    private static readonly Color GrayColor = Color.Gray;

    public RejuvenationCloakPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        _handler = (RejuvenationCloakHandler)item.EquipmentHandler!;
        Padding = new Thickness(20);
        MinWidth = 380;
        Spacing = 8;

        // ═══════════════════════════════════════════════════════════════════
        // Header Section: Icon + Description
        // ═══════════════════════════════════════════════════════════════════
        var headerSection = new HorizontalStackPanel { Spacing = 15, Margin = new Thickness(0, 0, 0, 12) };

        // Icon with frame
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 80, Height = 80
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 72, Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        // Description area
        var descArea = new VerticalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        if (item.Def.Description != "undefined")
        {
            descArea.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = item.Def.Description, Wrap = true, MaxWidth = 280
            });
        }

        // Current bonus display
        _bonusLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            TextColor = HealingColor
        };
        descArea.Widgets.Add(_bonusLabel);
        
        headerSection.Widgets.Add(descArea);
        Widgets.Add(headerSection);

        // ═══════════════════════════════════════════════════════════════════
        // Durability Section
        // ═══════════════════════════════════════════════════════════════════
        var durabilitySection = new VerticalStackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 12) };

        _durabilityBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Durability)
        {
            Width = 160, Height = 18,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        durabilitySection.Widgets.Add(_durabilityBar);

        _durabilityLabel = new Label("small")
        {
            Text = $"Durability: {item.Durability}/{item.MaxDurability}",
            TextColor = Color.LightGray
        };
        durabilitySection.Widgets.Add(_durabilityLabel);
        Widgets.Add(durabilitySection);

        // ═══════════════════════════════════════════════════════════════════
        // Properties Section
        // ═══════════════════════════════════════════════════════════════════
        var propsSection = new VerticalStackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 10) };
        propsSection.Widgets.Add(CreatePropertyRow("Slot", item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "n/a", Color.CornflowerBlue));
        
        _upgradeLevelLabel = new Label("small") { Text = $"{(int)_handler.UpgradeLevel} / 2", TextColor = GoldColor };
        propsSection.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small") { Text = "Upgrade Level:", TextColor = GrayColor },
                _upgradeLevelLabel
            }
        });
        Widgets.Add(propsSection);

        // ═══════════════════════════════════════════════════════════════════
        // Upgrade Section
        // ═══════════════════════════════════════════════════════════════════
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 8) });

        _upgradeSection = new VerticalStackPanel { Spacing = 6 };
        Widgets.Add(_upgradeSection);

        RefreshUpgradeSection();
        UpdateBonusLabel();
    }

    private void UpdateBonusLabel()
    {
        var bonus = _handler.CurrentBonusPercent;
        if (bonus > 0)
        {
            _bonusLabel.Text = $"Healing: +{bonus:F0}% bonus";
            _bonusLabel.TextColor = HealingColor;
        }
        else
        {
            _bonusLabel.Text = "Healing: Base rate";
            _bonusLabel.TextColor = GrayColor;
        }
    }

    private void RefreshUpgradeSection()
    {
        _upgradeSection.Widgets.Clear();
        _upgradeLevelLabel.Text = $"{(int)_handler.UpgradeLevel} / 2";

        // Section header
        _upgradeSection.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Upgrades",
            TextColor = GoldColor
        });

        var nextUpgrade = _handler.NextUpgrade;
        if (nextUpgrade == null)
        {
            _upgradeSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Fully upgraded!",
                TextColor = GoldColor,
                Margin = new Thickness(0, 4, 0, 0)
            });
            return;
        }

        var inventory = Core.Context.PlayerPawn.Inventory;
        var upgradeCost = _handler.GetUpgradeCost(nextUpgrade.Value);
        var canUpgrade = _handler.CanUpgrade(inventory);

        // Next upgrade header with bonus
        var levelNum = (int)nextUpgrade.Value;
        var bonusText = nextUpgrade.Value switch
        {
            RejuvenationCloakUpgradeLevel.Level1 => $"+{(int)((RejuvenationCloakHandler.Level1BonusMultiplier - 1f) * 100)}% Healing",
            RejuvenationCloakUpgradeLevel.Level2 => $"+{(int)((RejuvenationCloakHandler.Level2BonusMultiplier - 1f) * 100)}% Healing",
            _ => ""
        };

        _upgradeSection.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"Level {levelNum}: {bonusText}",
            TextColor = GoldColor,
            Margin = new Thickness(0, 4, 0, 4)
        });

        // Resource costs
        foreach (var cost in upgradeCost)
        {
            var hasEnough = inventory.AmountOf(cost.Item) >= cost.Count;
            var currentAmount = inventory.AmountOf(cost.Item);

            var costRow = new HorizontalStackPanel { Spacing = 8 };

            costRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(cost.Item.Texture),
                Width = 24, Height = 24,
                Opacity = hasEnough ? 1.0f : 0.5f,
                VerticalAlignment = VerticalAlignment.Center
            });

            costRow.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = $"{cost.Item.Label} {currentAmount}/{cost.Count}",
                TextColor = hasEnough ? Color.LightGreen : Color.IndianRed,
                VerticalAlignment = VerticalAlignment.Center
            });

            _upgradeSection.Widgets.Add(costRow);
        }

        // Upgrade button
        var upgradeButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label
            {
                Text = canUpgrade ? "Upgrade" : "Missing Materials",
                TextColor = canUpgrade ? GoldColor : GrayColor
            },
            Enabled = canUpgrade,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        upgradeButton.TouchDown += (_, _) =>
        {
            if (_handler.TryUpgrade(inventory))
            {
                RefreshUpgradeSection();
                UpdateBonusLabel();
            }
        };

        _upgradeSection.Widgets.Add(upgradeButton);
    }

    private static HorizontalStackPanel CreatePropertyRow(string key, string value, Color valueColor)
    {
        return new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small") { Text = $"{key}:", TextColor = GrayColor },
                new Label("small") { Text = value, TextColor = valueColor }
            }
        };
    }

    public override void Update()
    {
        _durabilityBar.Value = _item.Durability / _item.MaxDurability * 100;
        _durabilityLabel.Text = $"Durability: {_item.Durability:F0}/{_item.MaxDurability:F0}";
        RefreshUpgradeSection();
    }
}
