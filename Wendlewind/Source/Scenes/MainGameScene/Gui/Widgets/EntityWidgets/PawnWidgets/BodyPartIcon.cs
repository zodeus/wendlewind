using Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// Data for a single pip: its color, label for simple tooltip, and optional info panel.
/// </summary>
public readonly struct PipData
{
    public Color Color { get; init; }
    public string Label { get; init; }
    /// <summary>
    /// Optional custom info panel widget. If provided, shown instead of simple label tooltip.
    /// </summary>
    public Widget? InfoPanel { get; init; }
}

public sealed class BodyPartIcon : Panel
{
    // Number of individual modifier pips we will render under the icon.
    // Extra modifiers beyond this are ignored for now (still visible in the detailed panel).
    private const int MaxPips = 5;
    private const int IconDiameter = 36;
    private readonly ColoredRegion? _imageTexture;
    private readonly ColoredRegion _backgroundTexture;
    private readonly List<Panel> _pipWidgets = new();
    private event Action<BodyPartIcon>? Handler;

    public BodyPartIcon(ColoredRegion? imageTexture, Action<BodyPartIcon>? handler = null)
    {
        _imageTexture = imageTexture;
        var image = new Image { Background = imageTexture };
        Handler = handler;

        VerticalAlignment = VerticalAlignment.Center;
        Padding = new Thickness(0, 0, 0, 0);
        Width = IconDiameter;
        Height = IconDiameter;
        _backgroundTexture = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64], Color.White);
        Background = _backgroundTexture;

        image.VerticalAlignment = VerticalAlignment.Center;
        image.HorizontalAlignment = HorizontalAlignment.Center;
        image.Width = IconDiameter - 8;
        image.Height = IconDiameter - 8;
        
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
    public void SetPips(IReadOnlyList<PipData> pips)
    {
        // Clear existing pips
        foreach (var pip in _pipWidgets)
        {
            pip.RemoveFromParent();
        }
        _pipWidgets.Clear();

        if (pips.Count == 0)
        {
            return;
        }

        var pipCount = Math.Min(pips.Count, MaxPips);

        // Draw pips horizontally from the top-left of the icon container.
        const int pipDiameter = 12;
        const int pipSpacing = 2;
        const int maxPipsPerRow = 3;

        for (var i = 0; i < pipCount; i++)
        {
            var pipData = pips[i];
            var column = i % maxPipsPerRow;
            var row = i / maxPipsPerRow;
            var pipLeft = column * (pipDiameter + pipSpacing);
            var pipTop = row * (pipDiameter + pipSpacing);

            var pip = new Panel
            {
                Width = pipDiameter,
                Height = pipDiameter,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhiteFilled24],
                    pipData.Color),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Left = pipLeft,
                Top = pipTop
            };

            // Attach tooltip using TooltipHelper - use custom info panel if available, otherwise simple label
            var capturedPipData = pipData;
            pip.WithTooltip(() => capturedPipData.InfoPanel ?? new Label(BaseContent.Styles.Label.Small)
            {
                Text = capturedPipData.Label,
                TextColor = Color.GhostWhite
            });

            _pipWidgets.Add(pip);
            Widgets.Add(pip);
        }
    }

    /// <summary>
    /// Draw small colored pips around the outside of the circle to represent multiple modifiers.
    /// Overload for backwards compatibility - uses empty labels.
    /// </summary>
    public void SetPips(IReadOnlyList<Color> colors)
    {
        var pips = colors.Select(c => new PipData { Color = c, Label = "" }).ToList();
        SetPips(pips);
    }
}