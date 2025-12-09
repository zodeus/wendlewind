namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class ImageCircleIcon : Panel
{
    // Number of individual modifier pips we will render under the icon.
    // Extra modifiers beyond this are ignored for now (still visible in the detailed panel).
    private const int MaxPips = 5;

    private readonly ColoredRegion? _imageTexture;
    private readonly ColoredRegion _backgroundTexture;
    private readonly List<Panel> _pipWidgets = new();
    private event Action<ImageCircleIcon>? Handler;

    public ImageCircleIcon(ColoredRegion? imageTexture, Action<ImageCircleIcon>? handler = null)
    {
        _imageTexture = imageTexture;
        var image = new Image { Background = imageTexture };
        Handler = handler;
        Padding = new Thickness(8);
        Width = BaseContent.IconSizes.Medium;
        Height = BaseContent.IconSizes.Medium;
        _backgroundTexture = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64], Color.White);
        Background = _backgroundTexture;

        image.VerticalAlignment = VerticalAlignment.Center;
        image.HorizontalAlignment = HorizontalAlignment.Center;
        image.Width = BaseContent.IconSizes.Medium - 16;
        image.Height = BaseContent.IconSizes.Medium - 16;

        Widgets.Add(image);
    }

    public void Update()
    {
        Handler?.Invoke(this);
    }

    public void SetColor(Color color)
    {
        _backgroundTexture.Color = color;
        if (_imageTexture != null)
        {
            _imageTexture.Color = color;
        }
    }

    /// <summary>
    /// Draw small colored pips around the outside of the circle to represent multiple modifiers.
    /// </summary>
    public void SetPips(IReadOnlyList<Color> colors)
    {
        // Clear existing pips
        foreach (var pip in _pipWidgets)
        {
            pip.RemoveFromParent();
        }
        _pipWidgets.Clear();

        if (colors.Count == 0)
        {
            return;
        }

        var pipCount = Math.Min(colors.Count, MaxPips);

        // Draw pips horizontally from the top-left of the icon container.
        const int pipDiameter = 12;
        const int pipSpacing = 2;

        for (var i = 0; i < pipCount; i++)
        {
            var pip = new Panel
            {
                Width = pipDiameter,
                Height = pipDiameter,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhiteFilled24],
                    colors[i]),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Left = i * (pipDiameter + pipSpacing),
                Top = 0
            };

            _pipWidgets.Add(pip);
            Widgets.Add(pip);
        }
    }
}