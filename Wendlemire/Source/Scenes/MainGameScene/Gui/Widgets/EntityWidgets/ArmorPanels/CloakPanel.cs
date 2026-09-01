namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.ArmorPanels;

/// <summary>
/// A generic panel for displaying cloak items that have upgradable handlers.
/// Works with any handler that implements both IUpgradableHandler and ICloakHandler.
/// </summary>
[UsedImplicitly]
public sealed class CloakPanel : EntityPanelBase
{
    private readonly ICloakHandler? _cloakHandler;
    private readonly IUpgradableHandler? _upgradableHandler;
    private readonly ItemUpgradePanel? _upgradePanel;
    private readonly Label? _bonusLabel;
    private readonly Label? _upgradeLevelLabel;
    private readonly HorizontalStackPanel? _levelIndicatorContainer;

    public CloakPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        var handler = item.EquipmentHandler;
        if (handler is not ICloakHandler cloakHandler || handler is not IUpgradableHandler upgradableHandler)
        {
            BuildPreviewLayout(item);
            return;
        }

        _cloakHandler = cloakHandler;
        _upgradableHandler = upgradableHandler;
        
        Padding = new Thickness(20);
        MinWidth = 360;
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
            Background = item.GetIconImage(),
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
            Text = _cloakHandler.GetBonusDisplayText(),
            TextColor = Color.DarkGoldenrod
        };
        
        headerSection.Widgets.Add(descArea);
        Widgets.Add(headerSection);
        Widgets.Add(_bonusLabel);

        // ═══════════════════════════════════════════════════════════════════
        // Upgrade Level Box
        // ═══════════════════════════════════════════════════════════════════
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
        
        // Level indicator pips
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

        // ═══════════════════════════════════════════════════════════════════
        // Upgrade Section using ItemUpgradePanel
        // ═══════════════════════════════════════════════════════════════════
        _upgradePanel = new ItemUpgradePanel(item, _upgradableHandler, OnUpgradeComplete);
        Widgets.Add(_upgradePanel);

        UpdateBonusLabel();
    }

    private void BuildPreviewLayout(Item item)
    {
        Padding = new Thickness(20);
        MinWidth = 360;
        Spacing = 8;

        var headerSection = new HorizontalStackPanel { Spacing = 15, Margin = new Thickness(0, 0, 0, 12) };
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 80,
            Height = 80
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
            Width = 72,
            Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        if (!string.IsNullOrWhiteSpace(item.Def.Description) && item.Def.Description != "undefined")
        {
            headerSection.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = item.Def.Description,
                Wrap = true,
                MaxWidth = 280
            });
        }

        Widgets.Add(headerSection);
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small") { Text = "Slot:", TextColor = Color.Gray },
                new Label("small")
                {
                    Text = item.ItemDef.EquipmentProperties?.SlotUsedToEquip?.ToString() ?? "Cloak",
                    TextColor = ColorExt.HexToColor(TC.Blue.TrimStart('#'))
                }
            }
        });
        if (item.ItemDef.GoldCost > 0)
        {
            Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    new Label("small") { Text = "Cost:", TextColor = Color.Gray },
                    new Label("small")
                    {
                        Text = $"{item.ItemDef.GoldCost}g",
                        TextColor = ColorExt.HexToColor(TC.Golden.TrimStart('#'))
                    }
                }
            });
        }
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
        // No dynamic updates needed - upgrade panel handles its own updates
    }
}
