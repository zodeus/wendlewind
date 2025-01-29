namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakPawnsCreaturesPanel : Grid
{
    public BoakPawnsCreaturesPanel(IReadOnlyList<RaceDef> defs)
    {
        RowSpacing = 30;
        ColumnSpacing = 30;

        int gridRow = 0;
        int gridColum = 0;
        foreach (var def in defs)
        {
            var details = new VerticalStackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 0, 40, 0),
                Width = 600,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Large) { Text = def.Label, Margin = new Thickness(0, 0, 0, 20) },
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Species: {def.Species.Label}" },
                    new Label(BaseContent.Styles.Label.Normal) { Text = $"{def.Species.Description}" },
                }
            };

            var panel = new HorizontalStackPanel
            {
                Spacing = 10,
                Widgets =
                {
                    new Panel
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
                        Padding = new Thickness(10),
                        Widgets = { new Image { Width = 256, Height = 256, Background = new TextureRegion(def.Icon) } }
                    },
                    details
                }
            };
            SetRow(panel, gridRow);
            SetColumn(panel, gridColum);
            Widgets.Add(panel);

            gridColum++;
            if (gridColum > 2)
            {
                gridColum = 0;
                gridRow++;
            }
        }
    }
}