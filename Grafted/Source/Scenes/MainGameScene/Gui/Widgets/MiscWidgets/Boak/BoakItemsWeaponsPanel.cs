namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakItemsWeaponPanel : Grid
{
    public BoakItemsWeaponPanel(IReadOnlyList<ItemDef> defs, IReadOnlyList<WeaponManeuverDef> toolManeuverDefs)
    {
        defs = defs.OrderBy(d => d.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MeleePower)?.Value).ToList();
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Monkier" }, 0, 0);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Damage Type" }, 0, 1);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Damage" }, 0, 2);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Speed" }, 0, 3);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Durability" }, 0, 4);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Maneuvers" }, 0, 5);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Modifiers" }, 0, 6);

        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));


        int gridRow = 1;
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
                    new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Moniker}" }
                }
            }, gridRow, 0);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.WeaponProperties.DamageType}"
            }, gridRow, 1);
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MeleePower)?.Value}"
            }, gridRow, 2);
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.WeaponSpeed)?.Value}"
            }, gridRow, 3);
            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MaxDurability)?.Value}"
            }, gridRow, 4);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.Join(", ", def.WeaponProperties.BodyPartModifiers.Select(f => f.Def.Label))
            }, gridRow, 5);

            AddCell(new Label(BaseContent.Styles.Label.Medium)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.Join(", ", def.WeaponProperties.WeaponManeuvers.Select(f => f.Label))
            }, gridRow, 6);


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