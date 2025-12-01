namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakPawnsCreaturesPanel : Grid, IDisposable
{
    private readonly List<PawnBodyRenderWidget> _renderWidgets = new();

    public BoakPawnsCreaturesPanel(IReadOnlyList<PawnDef> defs)
    {
        RowSpacing = 30;
        ColumnSpacing = 30;

        var emptyLoadout = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout")!;

        int gridRow = 0;
        int gridColum = 0;
        foreach (var def in defs)
        {
            var details = new VerticalStackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 0, 40, 0),
                MinWidth = 250,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Normal) { Text = def.Label, Margin = new Thickness(0, 0, 0, 20) },
                    new Label(BaseContent.Styles.Label.Small) { Text = $"Species: {def.Species}" },
                    new Label(BaseContent.Styles.Label.Small) { Text = $"{def.Description}" },
                }
            };

            // Create a preview pawn to render
            var previewPawn = PawnGenerator.CreatePawn(new PawnRequest(
                def.Label,
                def,
                emptyLoadout,
                PawnType.Enemy
            ));

            var bodyWidget = new PawnBodyRenderWidget(previewPawn, 128)
            {
                Width = 128,
                Height = 128,
                VerticalAlignment = VerticalAlignment.Top
            };
            _renderWidgets.Add(bodyWidget);

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
                        Widgets = { bodyWidget }
                    },
                    details
                }
            };
            SetRow(panel, gridRow);
            SetColumn(panel, gridColum);
            Widgets.Add(panel);

            gridColum++;
            if (gridColum > 3)
            {
                gridColum = 0;
                gridRow++;
            }
        }
    }

    public void Dispose()
    {
        foreach (var widget in _renderWidgets)
        {
            widget.Dispose();
        }
        _renderWidgets.Clear();
    }
}