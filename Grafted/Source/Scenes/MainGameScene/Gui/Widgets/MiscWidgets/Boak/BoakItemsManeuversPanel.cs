namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsManeuversPanel : Grid
{
    public BoakItemsManeuversPanel(IReadOnlyList<WeaponManeuverDef> defs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        var column = 0;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Label" }, 0, column++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Dmg Multiplier" }, 0, column++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Weapons" }, 0, column);

        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        var gridRow = 1;
        foreach (var def in defs)
        {
            column = 0;
            AddCell(new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" }, gridRow, column++);
            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.DamageMultiplier}"
            }, gridRow, column++);
            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.Join(", ", def.Weapons?.Select(t => t.ToString()) ?? Array.Empty<string>())
            }, gridRow, column++);

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