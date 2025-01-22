namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsMedicalPanel : Grid
{
    public BoakItemsMedicalPanel(IReadOnlyList<ItemDef> defs)
    {
        //defs = defs.OrderBy(d => d.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.PhysicalResistance)?.Value).ToList();
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Label" }, 0, 0);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Healing Value" }, 0, 1);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Duration" }, 0, 2);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Description" }, 0, 3);

        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
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
                    new Panel
                    {
                        Width = 64,
                        VerticalAlignment = VerticalAlignment.Top,
                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                        Padding = new Thickness(4),
                        Widgets = { new Image { Width = 64, Height = 64, Background = new TextureRegion(def.Icon) } }
                    },
                    new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Label}" }
                }
            }, gridRow, 0);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.HealingValue)?.Value}"
            }, gridRow, 1);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.MedicinalProperties?.DurationInTicks}"
            }, gridRow, 2);

            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.Description}"
            }, gridRow, 3);

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