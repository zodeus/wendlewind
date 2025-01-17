using Grafted.Sim.Entities;
using Grafted.Sim.LootBoxes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakStatsPanel : Grid
{
    public BoakStatsPanel(IReadOnlyList<StatDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 30;
        ColumnSpacing = 30;

        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Label" }, 0, 0);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Min Value" }, 0, 1);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Max Value" }, 0, 2);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Base Value" }, 0, 3);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Factors" }, 0, 4);
        int gridRow = 1;
        foreach (var def in defs)
        {
            AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"{def.Label}" }, gridRow, 0);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"{def.MinValue}" }, gridRow, 1);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"{def.MaxValue}" }, gridRow, 2);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"{def.BaseValue}" }, gridRow, 3);
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                Text = string.Join(", ", def.StatFactors?.Select(f => f.Label) ?? [])
            }, gridRow, 4);


            gridRow++;
        }
    }

    private void AddCell(Widget widget, int row, int column)
    {
        SetRow(widget, row);
        SetColumn(widget, column);
        Widgets.Add(widget);
    }
}