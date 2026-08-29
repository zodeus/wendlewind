namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsBodyEffectsPanel : Grid
{
    public BoakPawnsBodyEffectsPanel(IReadOnlyList<BodyEffectDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        var gridColum = 0;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Label" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Notes" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Affected Stats" }, 0, gridColum++);

        DefaultColumnProportion = Proportion.Auto;

        var gridRow = 1;
        foreach (var def in defs)
        {
            gridColum = 0;
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Notes}" }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center, 
                Text = string.Join(", ", def.AffectedStats?.Select(s=>$"{s.Stat.Label}: {s.Factor}{s.Offset}") ?? Array.Empty<string>()) 
            }, gridRow, gridColum++);

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