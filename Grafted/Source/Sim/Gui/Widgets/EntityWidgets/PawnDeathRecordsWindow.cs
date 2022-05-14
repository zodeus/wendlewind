using Grafted.Sim.Entities.Pawns;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets;

public class PawnDeathRecordsWindow : Window {
    public PawnDeathRecordsWindow(PawnDeathRecords deathRecords) {
        Title = "Death Records";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        MinWidth = 500;
        MinHeight = 300;
        Padding = new Thickness(20);
        ScrollViewer scrollViewer = new();
        Grid container = new() { RowSpacing = 10, ColumnSpacing = 50, Margin = new Thickness(10), DefaultColumnProportion = Proportion.Auto, DefaultRowProportion = Proportion.Auto };
        scrollViewer.Content = container;
        Content = scrollViewer;
        container.AddChild(new Label { Text = "Cause of death", GridRow = 0, GridColumn = 1 });
        container.AddChild(new Label { Text = "Round", GridRow = 0, GridColumn = 2 });
        int gridRow = 1;
        foreach (DeathRecord deathRecord in deathRecords) {
            container.AddChild(new Label("small") { Text = deathRecord.PawnName, GridRow = gridRow, GridColumn = 0 });
            container.AddChild(new Label("small") { Text = deathRecord.CauseOfDeath, GridRow = gridRow, GridColumn = 1 });
            container.AddChild(new Label("small") { Text = deathRecord.Round.ToString(), GridRow = gridRow, GridColumn = 2 });
            gridRow++;
        }
    }
}