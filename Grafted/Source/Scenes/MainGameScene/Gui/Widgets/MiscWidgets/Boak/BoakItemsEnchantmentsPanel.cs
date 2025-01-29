namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsEnchantmentsPanel : Grid
{
    public BoakItemsEnchantmentsPanel(IReadOnlyList<ItemDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Label" }, 0, 0);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Description" }, 0, 1);

        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        var gridRow = 1;
        foreach (var def in defs)
        {
            AddCell(new HorizontalStackPanel
            {
                Spacing = 10,
                Widgets =
                {
                    new Image { Width = 64, Height = 64, Background = new TextureRegion(def.Icon) },
                    new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" }
                }
            }, gridRow, 0);

            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.Description}"
            }, gridRow, 1);

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