namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal class BoakPawnsBodyPartsPanel : Grid
{
    public BoakPawnsBodyPartsPanel(IReadOnlyList<BodyPartDef> partDefs, IReadOnlyList<BodyPartSocketDef> socketDefs)
    {
        Padding = new Thickness(16);
        RowSpacing = 20;
        ColumnSpacing = 50;

        var gridColum = 0;
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Label" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Blood" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Type" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Hit W." }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"IsVital" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"IsBone" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"IsFlesh" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"IsOrgan" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Mobility %" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Sockets" }, 0, gridColum++);
        AddCell(new Label(BaseContent.Styles.Label.Medium) { Text = $"Slots" }, 0, gridColum++);

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
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.BloodAmount}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.BodyPartType}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.HitWeight}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{(def.IsVital ? "Yes" : "")}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{(def.IsBone ? "Yes" : "")}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{(def.IsFlesh ? "Yes" : "")}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{(def.IsOrgan ? "Yes" : "")}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            AddCell(new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.MobilityFraction}", VerticalAlignment = VerticalAlignment.Center }, gridRow, gridColum++);
            
            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = string.Join(", ", def.Sockets?.Select(s => s.Label) ?? Array.Empty<string>()), VerticalAlignment = VerticalAlignment.Center
            }, gridRow, gridColum++);
            
            AddCell(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = string.Join(", ", def.EquipmentSlots?.Select(s => s.ToString()) ?? Array.Empty<string>()), VerticalAlignment = VerticalAlignment.Center
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