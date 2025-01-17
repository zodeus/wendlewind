namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsBloodsPanel : Grid
{
    public BoakPawnsBloodsPanel(IReadOnlyList<BloodDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        var gridColum = 0;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Label" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Color" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Viscosity" }, 0, gridColum++);

        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var gridRow = 1;
        foreach (var def in defs)
        {
            gridColum = 0;
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" }, gridRow, gridColum++);
            AddCell(new Image
            {
                Width = 96, Height = 32, Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.White], def.Color)
            }, gridRow, gridColum++);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.Viscosity}"
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