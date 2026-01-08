namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

/// <summary>
/// A reusable panel for displaying item upgrade information and handling upgrade interactions.
/// Works with any handler that implements IUpgradableHandler.
/// </summary>
public class ItemUpgradePanel : VerticalStackPanel
{
    private readonly Item _item;
    private readonly IUpgradableHandler _handler;
    private readonly Action? _onUpgradeComplete;
    
    private static readonly Color GoldColor = Color.Gold;
    private static readonly Color GrayColor = Color.Gray;

    public ItemUpgradePanel(Item item, IUpgradableHandler handler, Action? onUpgradeComplete = null)
    {
        _item = item;
        _handler = handler;
        _onUpgradeComplete = onUpgradeComplete;
        
        Spacing = 6;
        Refresh();
    }

    public void Refresh()
    {
        Widgets.Clear();
        
        var upgradeProps = _handler.UpgradeProperties;
        if (upgradeProps == null || upgradeProps.Upgrades.Count == 0)
        {
            return;
        }

        // Section header
        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Upgrades",
            TextColor = GoldColor
        });
        
        // Show current bonuses if upgraded
        if (_handler.UpgradeLevel > 0)
        {
            var currentBonusDesc = _handler.GetCurrentBonusDescription();
            if (!string.IsNullOrEmpty(currentBonusDesc))
            {
                Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = currentBonusDesc,
                    TextColor = Color.LightGreen
                });
            }
        }

        var nextUpgrade = upgradeProps.GetNextUpgrade(_handler.UpgradeLevel);
        if (nextUpgrade == null)
        {
            return;
        }

        var inventory = Core.Context.PlayerPawn.Inventory;
        var canUpgrade = _handler.CanUpgrade(inventory);

        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 4, 0, 4) });

        // Next upgrade header with bonus description
        var bonusText = nextUpgrade.BonusDescription;
        if (!string.IsNullOrEmpty(bonusText))
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Level {nextUpgrade.Level}: {bonusText}",
                TextColor = GoldColor,
            });
        }
        else
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Level {nextUpgrade.Level}",
                TextColor = GoldColor,
            });
        }

        // Required trinkets
        foreach (var trinketDef in nextUpgrade.RequiredTrinkets)
        {
            var hasTrinket = inventory.Trinkets.Any(t => t.Def == trinketDef);
            var trinketRow = new HorizontalStackPanel { Spacing = 6 };

            trinketRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(trinketDef.Texture),
                Width = 20, Height = 20,
                Opacity = hasTrinket ? 1.0f : 0.4f,
                VerticalAlignment = VerticalAlignment.Center
            });

            trinketRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = trinketDef.Label,
                TextColor = hasTrinket ? Color.LightGreen : Color.IndianRed,
                VerticalAlignment = VerticalAlignment.Center
            });

            Widgets.Add(trinketRow);
        }

        // Resource costs
        foreach (var cost in nextUpgrade.ResourceCosts)
        {
            var hasEnough = inventory.AmountOf(cost.Item) >= cost.Count;
            var currentAmount = inventory.AmountOf(cost.Item);

            var costRow = new HorizontalStackPanel { Spacing = 6 };

            costRow.Widgets.Add(new Image
            {
                Background = new TextureRegion(cost.Item.Texture),
                Width = 20, Height = 20,
                Opacity = hasEnough ? 1.0f : 0.5f,
                VerticalAlignment = VerticalAlignment.Center
            });

            costRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{cost.Item.Label} {currentAmount}/{cost.Count}",
                TextColor = hasEnough ? Color.LightGreen : Color.IndianRed,
                VerticalAlignment = VerticalAlignment.Center
            });

            Widgets.Add(costRow);
        }

        // Upgrade button
        var upgradeButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = canUpgrade ? "Upgrade" : "Missing Materials",
                TextColor = canUpgrade ? GoldColor : GrayColor,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            Enabled = canUpgrade,
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        upgradeButton.TouchDown += (_, _) =>
        {
            if (_handler.TryUpgrade(inventory))
            {
                Refresh();
                _onUpgradeComplete?.Invoke();
            }
        };

        Widgets.Add(upgradeButton);
    }
}
