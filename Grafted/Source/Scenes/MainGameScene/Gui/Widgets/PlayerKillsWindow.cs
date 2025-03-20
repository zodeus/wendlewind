namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public sealed class PlayerKillsWindow : Window
{
    public PlayerKillsWindow(PlayerKillRecords deathRecords)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        MinWidth = 500;
        MinHeight = 300;
        Padding = new Thickness(20);
        ScrollViewer scrollViewer = new();
        Grid container = new() { RowSpacing = 10, ColumnSpacing = 50, Margin = new Thickness(10), DefaultColumnProportion = Proportion.Auto, DefaultRowProportion = Proportion.Auto };
        scrollViewer.Content = container;
        Content = scrollViewer;
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Biome", GridRow = 0, GridColumn = 0 });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Creature", GridRow = 0, GridColumn = 1 });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Cause of death", GridRow = 0, GridColumn = 2 });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Damage", GridRow = 0, GridColumn = 3 });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Ticks", GridRow = 0, GridColumn = 4 });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Round", GridRow = 0, GridColumn = 5 });
        int gridRow = 1;
        int totalTicks = 0;
        double totalDamage = 0;
        foreach (DeathRecord deathRecord in deathRecords)
        {
            totalTicks += deathRecord.Ticks;
            totalDamage += deathRecord.TotalDamageDealt;

            var ticks = new Label
            {
                Text = $"{deathRecord.Ticks}", GridRow = gridRow, GridColumn = 4
            };
            var defaultColor = deathRecord.Ticks > 3000 ? Color.OrangeRed : ticks.TextColor;
            ticks.TextColor = defaultColor;

            container.Widgets.Add(new Label { Text = deathRecord.Biome.Label, GridRow = gridRow, GridColumn = 0 });
            container.Widgets.Add(new Label { Text = deathRecord.PawnName, GridRow = gridRow, GridColumn = 1 });
            container.Widgets.Add(new Label { Text = deathRecord.CauseOfDeath, GridRow = gridRow, GridColumn = 2 });
            container.Widgets.Add(new Label { Text = $"{deathRecord.TotalDamageDealt:N0}", GridRow = gridRow, GridColumn = 3 });
            container.Widgets.Add(ticks);
            container.Widgets.Add(new Label
            {
                TextColor = defaultColor,
                Text = deathRecord.Round.ToString(), GridRow = gridRow, GridColumn = 5
            });
            gridRow++;
        }


        container.Widgets.Add(new Label
        {
            Text = $"{totalDamage:N0}", GridRow = gridRow, GridColumn = 3
        });
        container.Widgets.Add(new Label
        {
            Text = $"{totalTicks:N0}", GridRow = gridRow, GridColumn = 4
        });
    }
}