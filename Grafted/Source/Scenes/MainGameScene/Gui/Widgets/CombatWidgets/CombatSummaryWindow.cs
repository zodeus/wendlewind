namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatSummaryWindow : Window
{
    public CombatSummaryWindow(Encounter encounter, Action onContinue)
    {
        var handler = encounter.CombatHandler!;
        var playerWon = !handler.Player.IsDead;
        
        TitlePanel.Visible = false;
        MinWidth = 500;
        Background = Stylesheet.Current.Atlas[
            playerWon ? BaseContent.Styles.Atlas.Panel.DeepGold : BaseContent.Styles.Atlas.Panel.Red
        ];

        var titleText = playerWon ? "Victory!" : "Defeat...";
        var titleColor = playerWon ? Color.Goldenrod : Color.IndianRed;
        
        var title = new Label(BaseContent.Styles.Label.Huge)
        {
            Text = titleText,
            TextColor = titleColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // Combat stats
        var statsPanel = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 20,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddStatRow(statsPanel, 0, "Enemy", handler.Enemy.LabelShort);
        AddStatRow(statsPanel, 1, "Duration", $"{encounter.Ticks} ticks");
        AddStatRow(statsPanel, 2, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}");

        // Loot preview (only if won and has loot)
        Widget? lootSection = null;
        if (playerWon && handler.Loot.Count() > 0)
        {
            var lootLabel = new Label(BaseContent.Styles.Label.Medium)
            {
                Text = "Loot",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 5)
            };
            
            var lootItems = new HorizontalStackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            foreach (var item in handler.Loot.Take(8))
            {
                var itemIcon = new Button(BaseContent.Styles.Button.Icon)
                {
                    Content = new Image
                    {
                        Background = new TextureRegion(item.Icon),
                        Width = 48,
                        Height = 48
                    },
                    Enabled = false
                };
                lootItems.Widgets.Add(itemIcon);
            }
            
            if (handler.Loot.Count() > 8)
            {
                var moreLabel = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"+{handler.Loot.Count() - 8} more",
                    VerticalAlignment = VerticalAlignment.Center
                };
                lootItems.Widgets.Add(moreLabel);
            }
            
            lootSection = new VerticalStackPanel
            {
                Widgets = { lootLabel, lootItems }
            };
        }

        // Severed limbs warning
        Widget? severedSection = null;
        if (handler.SeveredLimbs.Count > 0)
        {
            var severedLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Limbs lost: {string.Join(", ", handler.SeveredLimbs.Select(l => l.Label))}",
                TextColor = Color.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
                Wrap = true,
                Width = 400
            };
            severedSection = severedLabel;
        }

        var continueButton = new Button(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Continue", HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 25, 0, 0)
        };
        continueButton.Click += (_, _) =>
        {
            Close();
            onContinue();
        };

        var content = new VerticalStackPanel
        {
            Spacing = 5,
            Padding = new Thickness(40, 30, 40, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = { title, statsPanel }
        };
        
        if (lootSection != null)
            content.Widgets.Add(lootSection);
            
        if (severedSection != null)
            content.Widgets.Add(severedSection);
            
        content.Widgets.Add(continueButton);

        Content = content;
    }

    private static void AddStatRow(Grid grid, int row, string label, string value)
    {
        var labelWidget = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = label + ":",
            TextColor = Color.LightGray
        };
        Grid.SetRow(labelWidget, row);
        Grid.SetColumn(labelWidget, 0);
        grid.Widgets.Add(labelWidget);
        
        var valueWidget = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = value
        };
        Grid.SetRow(valueWidget, row);
        Grid.SetColumn(valueWidget, 1);
        grid.Widgets.Add(valueWidget);
    }
}
