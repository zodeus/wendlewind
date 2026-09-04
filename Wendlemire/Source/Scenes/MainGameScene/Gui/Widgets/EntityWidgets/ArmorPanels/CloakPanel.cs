namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.ArmorPanels;

/// <summary>
/// Inspect card for cloaks. Bonus text needs only <see cref="ICloakHandler"/>;
/// upgrade chrome is added when the handler is also <see cref="IUpgradableHandler"/>.
/// </summary>
[UsedImplicitly]
public sealed class CloakPanel : EntityPanelBase
{
    private readonly ICloakHandler? _cloakHandler;
    private readonly IUpgradableHandler? _upgradableHandler;
    private readonly Label? _bonusLabel;
    private ItemUpgradePanel? _upgradePanel;
    private Label? _upgradeLevelLabel;
    private HorizontalStackPanel? _levelIndicatorContainer;

    public CloakPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        var handler = item.EquipmentHandler;
        if (handler is not ICloakHandler cloakHandler)
        {
            BuildPreviewLayout(item);
            return;
        }

        _cloakHandler = cloakHandler;
        var card = EntityCardChrome.BeginInspect(this, item);

        _bonusLabel = new Label("small")
        {
            Text = _cloakHandler.GetBonusDisplayText(),
            TextColor = Color.DarkGoldenrod,
            Wrap = true,
            MaxWidth = card.BodyWidth
        };
        Widgets.Add(_bonusLabel);

        AddSlotAndCost(item);

        if (handler is IUpgradableHandler upgradableHandler)
        {
            _upgradableHandler = upgradableHandler;
            AddUpgradeChrome(item);
        }

        UpdateBonusLabel();
    }

    private void AddSlotAndCost(Item item)
    {
        var chips = new List<(string Key, string Value, Color Color)>
        {
            ("Slot", item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "Cloak", EntityCardChrome.Info)
        };
        if (item.ItemDef.GoldCost > 0)
        {
            chips.Add(("Cost", $"{item.ItemDef.GoldCost}g", EntityCardChrome.Gold));
        }

        Widgets.Add(EntityCardChrome.StatStrip(chips.ToArray()));
    }

    private void AddUpgradeChrome(Item item)
    {
        if (_upgradableHandler == null)
        {
            return;
        }

        var maxLevel = _upgradableHandler.UpgradeProperties?.MaxLevel ?? 2;
        var currentLevel = _upgradableHandler.UpgradeLevel;

        var levelBox = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var levelContent = new HorizontalStackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        levelContent.Widgets.Add(new Label("small")
        {
            Text = "UPGRADE",
            TextColor = Color.Gray,
            VerticalAlignment = VerticalAlignment.Center
        });

        _levelIndicatorContainer = new HorizontalStackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        BuildLevelIndicators(currentLevel, maxLevel);
        levelContent.Widgets.Add(_levelIndicatorContainer);

        _upgradeLevelLabel = new Label("small")
        {
            Text = $"{currentLevel} / {maxLevel}",
            TextColor = Color.DarkGoldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };
        levelContent.Widgets.Add(_upgradeLevelLabel);

        levelBox.Widgets.Add(levelContent);
        Widgets.Add(levelBox);

        _upgradePanel = new ItemUpgradePanel(item, _upgradableHandler, OnUpgradeComplete);
        Widgets.Add(_upgradePanel);
    }

    private void BuildPreviewLayout(Item item)
    {
        EntityCardChrome.BeginInspect(this, item);
        AddSlotAndCost(item);
    }

    private void BuildLevelIndicators(int currentLevel, int maxLevel)
    {
        if (_levelIndicatorContainer == null)
        {
            return;
        }

        _levelIndicatorContainer.Widgets.Clear();
        for (var i = 1; i <= maxLevel; i++)
        {
            var filled = i <= currentLevel;
            var pip = new Panel
            {
                Width = 12,
                Height = 12,
                Background = filled
                    ? new SolidBrush(Color.DarkGoldenrod)
                    : new SolidBrush(new Color(60, 60, 60))
            };
            _levelIndicatorContainer.Widgets.Add(pip);
        }
    }

    private void OnUpgradeComplete()
    {
        if (_upgradableHandler == null || _upgradeLevelLabel == null)
        {
            return;
        }

        UpdateBonusLabel();
        var maxLevel = _upgradableHandler.UpgradeProperties?.MaxLevel ?? 2;
        var currentLevel = _upgradableHandler.UpgradeLevel;
        _upgradeLevelLabel.Text = $"{currentLevel} / {maxLevel}";
        BuildLevelIndicators(currentLevel, maxLevel);
    }

    private void UpdateBonusLabel()
    {
        if (_bonusLabel == null || _cloakHandler == null)
        {
            return;
        }

        _bonusLabel.Text = _cloakHandler.GetBonusDisplayText();
    }

    public override void Update()
    {
        UpdateBonusLabel();
    }
}
