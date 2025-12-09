using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// Data for a single pip: its color and label for the tooltip.
/// </summary>
public readonly struct PipData
{
    public Color Color { get; init; }
    public string Label { get; init; }
}

public sealed class ImageCircleIcon : Panel
{
    // Number of individual modifier pips we will render under the icon.
    // Extra modifiers beyond this are ignored for now (still visible in the detailed panel).
    private const int MaxPips = 5;
    private const int IconDiameter = 38;
    private readonly ColoredRegion? _imageTexture;
    private readonly ColoredRegion _backgroundTexture;
    private readonly List<Panel> _pipWidgets = new();
    private Label? _tooltipLabel;
    private event Action<ImageCircleIcon>? Handler;

    public ImageCircleIcon(ColoredRegion? imageTexture, Action<ImageCircleIcon>? handler = null)
    {
        _imageTexture = imageTexture;
        var image = new Image { Background = imageTexture };
        Handler = handler;
        Padding = new Thickness(8);
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
        // Clear existing pips and tooltip
        foreach (var pip in _pipWidgets)
        {
            pip.RemoveFromParent();
        }
        _pipWidgets.Clear();
        
        _tooltipLabel?.RemoveFromParent();
        _tooltipLabel = null;

        if (pips.Count == 0)
        {
            return;
        }

        var pipCount = Math.Min(pips.Count, MaxPips);

        // Draw pips horizontally from the top-left of the icon container.
        const int pipDiameter = 12;
        const int pipSpacing = 2;

        // Create a shared tooltip label (initially hidden)
        _tooltipLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Visible = false,
            TextColor = Color.White,
            Padding = new Thickness(6, 3, 6, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Top = -20,
        };

        for (var i = 0; i < pipCount; i++)
        {
            var pipData = pips[i];
            var pip = new Panel
            {
                Width = pipDiameter,
                Height = pipDiameter,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhiteFilled24],
                    pipData.Color),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Left = i * (pipDiameter + pipSpacing),
                Top = 0
            };

            // Add hover handlers to show/hide tooltip
            var label = pipData.Label;
            var pipLeft = i * (pipDiameter + pipSpacing);
            pip.MouseEntered += (_, _) =>
            {
                if (_tooltipLabel != null)
                {
                    _tooltipLabel.Text = label;
                    _tooltipLabel.Left = pipLeft;
                    _tooltipLabel.Visible = true;
                    _tooltipLabel.TextColor = Color.GhostWhite;
                }
            };

            pip.MouseLeft += (_, _) =>
            {
                if (_tooltipLabel != null)
                {
                    _tooltipLabel.Visible = false;
                }
            };

            _pipWidgets.Add(pip);
            Widgets.Add(pip);
        }

        // Add tooltip last so it renders on top
        Widgets.Add(_tooltipLabel);
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