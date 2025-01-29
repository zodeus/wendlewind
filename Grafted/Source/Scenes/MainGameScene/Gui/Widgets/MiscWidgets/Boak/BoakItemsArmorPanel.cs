namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsArmorPanel : Grid
{
    public BoakItemsArmorPanel(IReadOnlyList<ItemDef> defs)
    {
        defs = defs.OrderBy(d => d.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.PhysicalResistance)?.Value).ToList();
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Label" }, 0, 0);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Physical Res" }, 0, 1);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Durability" }, 0, 2);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Slot" }, 0, 3);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = "Modifiers" }, 0, 4);

        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));

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
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.PhysicalResistance)?.Value}"
            }, gridRow, 1);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MaxDurability)?.Value}"
            }, gridRow, 2);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.EquipmentProperties?.SlotUsedToEquip}"
            }, gridRow, 3);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.Join(", ", def.WeaponProperties?.BodyPartModifiers.Select(f => f.Def.Label) ?? [])
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