namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class PlayerKillsWindow : Window
{
    public PlayerKillsWindow(PlayerKillRecords deathRecords)
    {
        Title = "Kills";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        MinWidth = 500;
        MinHeight = 300;
        Padding = new Thickness(20);
        ScrollViewer scrollViewer = new();
        Grid container = new() { RowSpacing = 10, ColumnSpacing = 50, Margin = new Thickness(10), DefaultColumnProportion = Proportion.Auto, DefaultRowProportion = Proportion.Auto };
        scrollViewer.Content = container;
        Content = scrollViewer;
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Cause of death", GridRow = 0, GridColumn = 1 });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Round", GridRow = 0, GridColumn = 2 });
        int gridRow = 1;
        foreach (DeathRecord deathRecord in deathRecords)
        {
            container.Widgets.Add(new Label() { Text = deathRecord.PawnName, GridRow = gridRow, GridColumn = 0 });
            container.Widgets.Add(new Label() { Text = deathRecord.CauseOfDeath, GridRow = gridRow, GridColumn = 1 });
            container.Widgets.Add(new Label() { Text = deathRecord.Round.ToString(), GridRow = gridRow, GridColumn = 2 });
            gridRow++;
        }
    }
}