namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsBodiesPanel : Grid
{
    public BoakPawnsBodiesPanel(IReadOnlyList<BodyDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        var gridColum = 0;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Label" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "BoneDensity" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "BloodType" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "MaxBlood" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "MaxEnergy" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Generator" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Handler" }, 0, gridColum++);

        DefaultColumnProportion = Proportion.Auto;

        var gridRow = 1;
        foreach (var def in defs)
        {
            gridColum = 0;
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.BoneDensity}" }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.BloodType}" }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.MaxBlood}" }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.MaxEnergy}" }, gridRow, gridColum++);
            
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.GeneratorClass.Name}"
            }, gridRow, gridColum++);            
            
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.HandlerClass.Name}"
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