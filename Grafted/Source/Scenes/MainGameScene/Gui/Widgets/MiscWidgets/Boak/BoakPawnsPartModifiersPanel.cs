namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsPartModifiersPanel : Grid
{
    public BoakPawnsPartModifiersPanel(IReadOnlyList<BodyPartModifierDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        var gridColum = 0;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Label" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Type" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Color" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Color Priority" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Handler" }, 0, gridColum++);

        DefaultColumnProportion = Proportion.Auto;

        var gridRow = 1;
        foreach (var def in defs)
        {
            gridColum = 0;
            var labelWidget = new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" };
            labelWidget.WithTooltip(() => BodyPartModifierGenerator.Generate(def, 666, 3).GetInfoPanel()! ?? new Label(BaseContent.Styles.Label.Small) { Text = "No info panel", TextColor = Color.GhostWhite });
            AddCell(labelWidget, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Type}" }, gridRow, gridColum++);
            AddCell(new Image
            {
                Width = 96, Height = 32, Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.White], def.Color)
            }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.ColorPriority}" }, gridRow, gridColum++);

            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.HandlerClass.Name}" }, gridRow, gridColum++);

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