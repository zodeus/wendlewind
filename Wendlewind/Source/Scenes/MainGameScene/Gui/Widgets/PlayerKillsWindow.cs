namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets;

public sealed class PlayerKillsWindow : Window
{
    private static PlayerKillsWindow? _instance;
    
    private static readonly Color HeaderColor = Color.Goldenrod;
    private static readonly Color ValueColor = Color.White;
    private static readonly Color SubduedColor = Color.Gray;
    private static readonly Color WarningColor = Color.OrangeRed;
    private static readonly Color AccentColor = new(100, 180, 100);

    public static void Toggle(Desktop desktop, PlayerKillRecords deathRecords)
    {
        if (_instance?.IsPlaced == true)
        {
            _instance.Close();
            return;
        }
        
        _instance = new PlayerKillsWindow(deathRecords);
        _instance.Show(desktop);
    }

    private PlayerKillsWindow(PlayerKillRecords deathRecords)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        MinWidth = 1200;
        MinHeight = 800;
        Padding = new Thickness(25);
        Content = BuildContent(deathRecords);
    }

    private Widget BuildContent(PlayerKillRecords deathRecords)
    {
        var mainPanel = new VerticalStackPanel
        {
            Spacing = 15
        };

        // Summary stats at the top
        var summaryPanel = BuildSummaryPanel(deathRecords);
        mainPanel.Widgets.Add(summaryPanel);

        mainPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });

        // Kill table
        var tableSection = BuildKillTable(deathRecords);
        mainPanel.Widgets.Add(tableSection);

        return mainPanel;
    }

    private Widget BuildSummaryPanel(PlayerKillRecords deathRecords)
    {
        var totalKills = deathRecords.List.Count;
        var totalDamage = deathRecords.Sum(r => r.TotalDamageDealt);
        var totalTicks = deathRecords.Sum(r => r.Ticks);
        var avgTicksPerKill = totalKills > 0 ? totalTicks / totalKills : 0;

        var summaryGrid = new Grid
        {
            RowSpacing = 4,
            ColumnSpacing = 40,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 5)
        };

        AddSummaryCell(summaryGrid, 0, 0, "Total Kills", $"{totalKills}", AccentColor);
        AddSummaryCell(summaryGrid, 0, 1, "Total Damage", $"{totalDamage:N0}", HeaderColor);
        AddSummaryCell(summaryGrid, 0, 2, "Total Ticks", $"{totalTicks:N0}", ValueColor);
        AddSummaryCell(summaryGrid, 0, 3, "Avg Ticks/Kill", $"{avgTicksPerKill:N0}", SubduedColor);

        return summaryGrid;
    }

    private static void AddSummaryCell(Grid grid, int row, int col, string label, string value, Color valueColor)
    {
        var cell = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 2
        };

        cell.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = label,
            TextColor = SubduedColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        cell.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = value,
            TextColor = valueColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, col);
        grid.Widgets.Add(cell);
    }

    private Widget BuildKillTable(PlayerKillRecords deathRecords)
    {
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 450,
            ShowVerticalScrollBar = true
        };

        var tableGrid = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto,
            Margin = new Thickness(5)
        };

        // Column headers
        AddTableHeader(tableGrid, 0, "#");
        AddTableHeader(tableGrid, 1, "Biome");
        AddTableHeader(tableGrid, 2, "Creature");
        AddTableHeader(tableGrid, 3, "Cause of Death");
        AddTableHeader(tableGrid, 4, "Weapon");
        AddTableHeader(tableGrid, 5, "Maneuver");
        AddTableHeader(tableGrid, 6, "Damage");
        AddTableHeader(tableGrid, 7, "Ticks");

        // Data rows
        int rowIndex = 1;
        foreach (var record in deathRecords)
        {
            var isSlowKill = record.Ticks > 3000;
            var rowColor = isSlowKill ? WarningColor : ValueColor;
            var zoneColor = record.ZoneDef.ZoneColor;

            AddTableCell(tableGrid, rowIndex, 0, $"{record.Round}", SubduedColor);
            AddTableCell(tableGrid, rowIndex, 1, record.ZoneDef.Label, zoneColor);
            AddTableCell(tableGrid, rowIndex, 2, record.PawnName, ValueColor);
            AddTableCell(tableGrid, rowIndex, 3, record.CauseOfDeath, SubduedColor);
            AddTableCell(tableGrid, rowIndex, 4, record.KillingWeapon ?? "—", AccentColor);
            AddTableCell(tableGrid, rowIndex, 5, record.KillingManeuver ?? "—", AccentColor);
            AddTableCell(tableGrid, rowIndex, 6, $"{record.TotalDamageDealt:N0}", HeaderColor);
            AddTableCell(tableGrid, rowIndex, 7, $"{record.Ticks}", rowColor);

            rowIndex++;
        }

        // Empty state
        if (!deathRecords.Any())
        {
            var emptyLabel = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = "No kills recorded yet",
                TextColor = SubduedColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 30)
            };
            Grid.SetRow(emptyLabel, 1);
            Grid.SetColumnSpan(emptyLabel, 8);
            tableGrid.Widgets.Add(emptyLabel);
        }

        scrollViewer.Content = tableGrid;
        return scrollViewer;
    }

    private static void AddTableHeader(Grid grid, int column, string text)
    {
        var label = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = text,
            TextColor = HeaderColor
        };
        Grid.SetRow(label, 0);
        Grid.SetColumn(label, column);
        grid.Widgets.Add(label);
    }

    private static void AddTableCell(Grid grid, int row, int column, string text, Color color)
    {
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = text,
            TextColor = color
        };
        Grid.SetRow(label, row);
        Grid.SetColumn(label, column);
        grid.Widgets.Add(label);
    }
}
