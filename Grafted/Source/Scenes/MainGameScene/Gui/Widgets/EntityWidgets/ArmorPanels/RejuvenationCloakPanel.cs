namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.ArmorPanels;

[UsedImplicitly]
public sealed class RejuvenationCloakPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly RejuvenationCloakHandler _handler;
    private readonly Label _durabilityLabel;
    private readonly HorizontalProgressBar _durabilityBar;
    private readonly ItemUpgradePanel _upgradePanel;
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
        
        _upgradeLevelLabel = new Label("small") { Text = $"{_handler.UpgradeLevel} / 2", TextColor = GoldColor };
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
        // Upgrade Section using ItemUpgradePanel
        // ═══════════════════════════════════════════════════════════════════
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 8) });

        _upgradePanel = new ItemUpgradePanel(item, _handler, OnUpgradeComplete);
        Widgets.Add(_upgradePanel);

        UpdateBonusLabel();
    }
    
    private void OnUpgradeComplete()
    {
        UpdateBonusLabel();
        _upgradeLevelLabel.Text = $"{_handler.UpgradeLevel} / 2";
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
    }
}
