using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// Displays a pawn's stats (Strength, Agility, etc.) in a two-column grid layout.
/// </summary>
public sealed class PawnStatsPanel : VerticalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly Dictionary<BaseStat, Label> _statLabels = new();

    public PawnStatsPanel(Pawn pawn)
    {
        _pawn = pawn;
        Spacing = 8;
        Margin = new Thickness(0, 12, 0, 0);

        if (!pawn.Def.BaseStats.Any())
            return;

        // Section header with decorative line
        var headerRow = new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        headerRow.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Base Stats",
            TextColor = new Color(200, 170, 100)
        });

        var headerLine = new Panel
        {
            Height = 1,
            Background = new SolidBrush(new Color(80, 70, 55)),
            VerticalAlignment = VerticalAlignment.Center
        };
        HorizontalStackPanel.SetProportionType(headerLine, ProportionType.Fill);
        headerRow.Widgets.Add(headerLine);

        Widgets.Add(headerRow);

        // Stats grid container
        var statsContainer = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 8,
            DefaultRowProportion = Proportion.Auto,
            Margin = new Thickness(0, 4, 0, 0)
        };
        statsContainer.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        statsContainer.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        statsContainer.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 40));
        statsContainer.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        statsContainer.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var stats = pawn.Def.BaseStats.ToList();
        var halfCount = (stats.Count + 1) / 1;

        for (var i = 0; i < stats.Count; i++)
        {
            var stat = stats[i];
            var isRightColumn = i >= halfCount;
            var gridRow = isRightColumn ? i - halfCount : i;
            var nameCol = isRightColumn ? 3 : 0;
            var valueCol = isRightColumn ? 4 : 1;

            var nameLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = stat.Def.Label,
                TextColor = new Color(170, 165, 155)
            };
            Grid.SetRow(nameLabel, gridRow);
            Grid.SetColumn(nameLabel, nameCol);
            statsContainer.Widgets.Add(nameLabel);

            var valueLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{pawn.GetStatValue(stat.Def):F2}",
                TextColor = new Color(200, 180, 120),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(valueLabel, gridRow);
            Grid.SetColumn(valueLabel, valueCol);
            statsContainer.Widgets.Add(valueLabel);

            var descriptionLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = stat.Def.Description,
                TextColor = new Color(170, 165, 155),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(descriptionLabel, gridRow);
            Grid.SetColumn(descriptionLabel, 2);
            statsContainer.Widgets.Add(descriptionLabel);

            _statLabels[stat] = valueLabel;
        }

        Widgets.Add(statsContainer);
    }

    public void Update()
    {
        foreach (var (stat, label) in _statLabels)
        {
            label.Text = $"{_pawn.GetStatValue(stat.Def):F2}";
        }
    }
}
