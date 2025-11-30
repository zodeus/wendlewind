namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsBodyPartsPanel : Grid
{
    public BoakPawnsBodyPartsPanel(IReadOnlyList<BodyPartDef> partDefs, IReadOnlyList<BodyPartSocketDef> socketDefs)
    {
        Padding = new Thickness(16);
        RowSpacing = 0;
        ColumnSpacing = 30;

        var gridColum = 0;
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Label" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "HitPoints" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Blood" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Type" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Hit W." }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "IsVital" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Substance" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "IsOrgan" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Mobility %" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = "Slots" }, 0, gridColum++);

        DefaultColumnProportion = Proportion.Auto;

        var gridRow = 1;
        foreach (var def in partDefs)
        {
            gridColum = 0;
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
                    new Label(BaseContent.Styles.Label.Normal) { VerticalAlignment = VerticalAlignment.Center, Text = $"{def.Moniker}" }
                }
            }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = $"{def.BaseStats.FirstOrNull(s => s?.Def == Defs.Stats.MaxHitPoints)?.Value}", VerticalAlignment = VerticalAlignment.Center
            }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.BloodAmount}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.BodyPartType}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.HitWeight}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{(def.IsVital ? "Yes" : "")}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.Substance}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{(def.IsOrgan ? "Yes" : "")}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.MobilityFraction}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = string.Join(", ", def.EquipmentSlots?.Select(s => s.ToString()) ?? Array.Empty<string>()), VerticalAlignment = VerticalAlignment.Center
            }, gridRow, gridColum++);

            gridRow++;

            AddCell(new Label(BaseContent.Styles.Label.Small)
            {
                Text = string.Join(", ", def.Sockets?.Select(s => s.Label) ?? Array.Empty<string>()), VerticalAlignment = VerticalAlignment.Center,
            }, gridRow, 0, 10);
            gridRow++;
            AddCell(new HorizontalSeparator { Margin = new Thickness(0, 0, 0, 10) }, gridRow, 0, 10);
            gridRow++;
        }
    }

    private void AddCell(Widget widget, int row, int column, int colSpan = 1)
    {
        SetRow(widget, row);
        SetColumn(widget, column);
        SetColumnSpan(widget, colSpan);
        Widgets.Add(widget);
    }
}