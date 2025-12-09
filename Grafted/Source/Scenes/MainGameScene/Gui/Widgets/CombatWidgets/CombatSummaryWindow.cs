using Grafted.Sim.Entities.Pawns;

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

        if (playerWon)
        {
            Content = BuildVictoryContent(encounter, handler, onContinue);
        }
        else
        {
            Content = BuildDeathReportContent(encounter, handler);
        }
    }

    private Widget BuildVictoryContent(Encounter encounter, CombatHandler handler, Action onContinue)
    {
        var title = new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "Victory!",
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var statsPanel = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 20,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddStatRow(statsPanel, 0, "Opponent", handler.Enemy.LabelShort);
        AddStatRow(statsPanel, 1, "Duration", $"{encounter.Ticks} ticks");
        AddStatRow(statsPanel, 2, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}");
        
        var rowIndex = 3;
        if (handler.CauseOfDeath != null)
        {
            AddStatRow(statsPanel, rowIndex++, "Cause of Death", handler.CauseOfDeath);
        }
        
        if (handler.KillingWeapon != null)
        {
            AddStatRow(statsPanel, rowIndex++, "Killing Blow", handler.KillingWeapon);
        }
        
        if (handler.KillingManeuver != null)
        {
            AddStatRow(statsPanel, rowIndex++, "Maneuver", handler.KillingManeuver);
        }

        Widget? lootSection = null;
        if (handler.Loot.Count() > 0)
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

        return content;
    }

    private Widget BuildDeathReportContent(Encounter encounter, CombatHandler handler)
    {
        var deathRecords = Core.Context.DeathRecords.List;
        var totalDamageAllRuns = deathRecords.Sum(r => r.TotalDamageDealt) + handler.TotalDirectPlayerDamage;
        var totalKills = deathRecords.Count;
        
        // Title
        var title = new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "DEATH REPORT",
            TextColor = new Color(180, 30, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Subtitle with cause of death
        var causeLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"Killed by: {handler.CauseOfDeath ?? "Unknown"}",
            TextColor = Color.IndianRed,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // Final Combat Stats Section
        var finalCombatTitle = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "— FINAL BATTLE —",
            TextColor = Color.DarkGray,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 8)
        };

        var finalStatsPanel = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddStatRow(finalStatsPanel, 0, "Slain by", handler.Enemy.LabelShort, Color.OrangeRed);
        AddStatRow(finalStatsPanel, 1, "Location", encounter.Zone.ZoneDef.Label, Color.LightGray);
        AddStatRow(finalStatsPanel, 2, "Combat Duration", $"{encounter.Ticks} ticks", Color.LightGray);
        AddStatRow(finalStatsPanel, 3, "Damage Dealt", $"{handler.TotalDirectPlayerDamage:N0}", Color.Goldenrod);

        // Run Summary Section
        var runTitle = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "— RUN SUMMARY —",
            TextColor = Color.DarkGray,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 8)
        };

        var runStatsPanel = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        AddStatRow(runStatsPanel, 0, "Enemies Defeated", $"{totalKills}", Color.Goldenrod);
        AddStatRow(runStatsPanel, 1, "Total Damage", $"{totalDamageAllRuns:N0}", Color.Goldenrod);

        // Kill History Section (last 5 kills)
        Widget? killHistorySection = null;
        if (deathRecords.Count > 0)
        {
            var killHistoryTitle = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = "— KILL HISTORY —",
                TextColor = Color.DarkGray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 8)
            };

            var killList = new VerticalStackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (var record in deathRecords.TakeLast(6))
            {
                var killEntry = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"#{record.Round} {record.PawnName} — {record.CauseOfDeath}",
                    TextColor = Color.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                killList.Widgets.Add(killEntry);
            }

            killHistorySection = new VerticalStackPanel
            {
                Widgets = { killHistoryTitle, killList }
            };
        }

        // Severed limbs
        Widget? severedSection = null;
        if (handler.SeveredLimbs.Count > 0)
        {
            var severedLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Limbs lost in final battle: {string.Join(", ", handler.SeveredLimbs.Select(l => l.Label))}",
                TextColor = Color.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 0),
                Wrap = true,
                Width = 450
            };
            severedSection = severedLabel;
        }

        // Restart button
        var restartButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label { Text = "Try Again", HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 30, 0, 0)
        };
        restartButton.Click += (_, _) =>
        {
            Close();
            Core.Context.StartOver();
        };

        // Build content
        var content = new VerticalStackPanel
        {
            Spacing = 0,
            Padding = new Thickness(50, 35, 50, 35),
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = 
            { 
                title, 
                causeLabel,
                finalCombatTitle,
                finalStatsPanel,
                runTitle,
                runStatsPanel
            }
        };

        if (killHistorySection != null)
            content.Widgets.Add(killHistorySection);

        if (severedSection != null)
            content.Widgets.Add(severedSection);

        content.Widgets.Add(restartButton);

        return content;
    }

    private static void AddStatRow(Grid grid, int row, string label, string value, Color? valueColor = null)
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
            Text = value,
            TextColor = valueColor ?? Color.White
        };
        Grid.SetRow(valueWidget, row);
        Grid.SetColumn(valueWidget, 1);
        grid.Widgets.Add(valueWidget);
    }
}
