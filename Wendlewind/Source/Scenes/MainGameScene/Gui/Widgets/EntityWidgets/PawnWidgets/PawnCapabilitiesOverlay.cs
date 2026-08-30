using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// A compact capabilities overlay for display within the pawn renderer.
/// Shows Sight, Breathing, Circulation, and Mobility as horizontal bars.
/// </summary>
public sealed class PawnCapabilitiesOverlay : Panel, IUpdatable
{
    private readonly PawnCapabilities _capabilities;
    private readonly Dictionary<string, CapabilityRow> _rows = new();
    private readonly Dictionary<Color, SolidBrush> _brushes = new();

    // Color palette matching the existing capabilities panel
    private static readonly Color ColorExcellent = new(120, 200, 80);
    private static readonly Color ColorGood = new(180, 200, 80);
    private static readonly Color ColorWeak = new(220, 160, 60);
    private static readonly Color ColorCritical = new(200, 80, 80);
    private static readonly Color ColorDisabled = new(100, 95, 90);

    // Semi-transparent background for overlay effect
    private static readonly Color BackgroundColor = new(20, 18, 15, 220);
    private static readonly Color BorderColor = new(80, 70, 55);
    private static readonly Color HeaderColor = new(200, 170, 100);
    private static readonly Color LabelColor = new(170, 165, 155);

    public PawnCapabilitiesOverlay(PawnBody body)
    {
        _capabilities = body.Capabilities;

        // Main container with semi-transparent background
        Background = new SolidBrush(BackgroundColor);
        Border = new SolidBrush(BorderColor);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8, 6, 8, 8);

        var container = new VerticalStackPanel { Spacing = 4 };
        Widgets.Add(container);

        // Header row
        var headerRow = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 2)
        };

        headerRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Capabilities",
            TextColor = HeaderColor
        });

        var headerLine = new Panel
        {
            Height = 1,
            Background = new SolidBrush(BorderColor),
            VerticalAlignment = VerticalAlignment.Center
        };
        HorizontalStackPanel.SetProportionType(headerLine, ProportionType.Fill);
        headerRow.Widgets.Add(headerLine);

        container.Widgets.Add(headerRow);

        // Capability rows container
        var rowsContainer = new VerticalStackPanel { Spacing = 3 };
        container.Widgets.Add(rowsContainer);

        // Add capability rows
        rowsContainer.Widgets.Add(CreateCapabilityRow("Sight", _capabilities.Sight));
        rowsContainer.Widgets.Add(CreateCapabilityRow("Breathing", _capabilities.Breathing));
        rowsContainer.Widgets.Add(CreateCapabilityRow("Circulation", _capabilities.Circulation));
        rowsContainer.Widgets.Add(CreateCapabilityRow("Mobility", _capabilities.Mobility));
    }

    private Widget CreateCapabilityRow(string name, float value)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Status indicator dot
        var indicator = new Panel
        {
            Width = 5,
            Height = 5,
            Background = Brush(GetStatusColor(value)),
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Widgets.Add(indicator);

        // Capability name
        var nameLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = name,
            TextColor = LabelColor,
            Width = 90,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Widgets.Add(nameLabel);

        // Progress bar background
        var barBackground = new Panel
        {
            Width = 60,
            Height = 6,
            Background = new SolidBrush(new Color(35, 32, 28)),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Progress bar fill
        var fillWidth = Math.Clamp((int)(value * 60), 0, 60);
        var barFill = new Panel
        {
            Width = fillWidth,
            Height = 6,
            Background = Brush(GetBarColor(value)),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        barBackground.Widgets.Add(barFill);
        row.Widgets.Add(barBackground);

        _rows[name] = new CapabilityRow(barFill, indicator, value);
        return row;
    }

    private static string FormatValue(float value)
    {
        if (float.IsNaN(value)) return "n/a";
        return $"{(int)(value * 100)}%";
    }

    private static Color GetStatusColor(float value)
    {
        if (float.IsNaN(value)) return ColorDisabled;
        return value switch
        {
            >= 1f => ColorExcellent,
            >= 0.6f => ColorGood,
            >= 0.3f => ColorWeak,
            > 0f => ColorCritical,
            _ => ColorDisabled
        };
    }

    private static Color GetValueColor(float value)
    {
        if (float.IsNaN(value)) return ColorDisabled;
        return value switch
        {
            >= 1f => ColorExcellent,
            >= 0.6f => ColorGood,
            >= 0.3f => ColorWeak,
            > 0f => ColorCritical,
            _ => ColorDisabled
        };
    }

    private static Color GetBarColor(float value)
    {
        if (float.IsNaN(value)) return new Color(45, 42, 38);
        return value switch
        {
            >= 1f => new Color(100, 170, 70),
            >= 0.6f => new Color(160, 180, 70),
            >= 0.3f => new Color(200, 140, 50),
            > 0f => new Color(180, 70, 70),
            _ => new Color(45, 42, 38)
        };
    }

    public void Update()
    {
        UpdateCapability("Sight", _capabilities.Sight);
        UpdateCapability("Breathing", _capabilities.Breathing);
        UpdateCapability("Circulation", _capabilities.Circulation);
        UpdateCapability("Mobility", _capabilities.Mobility);
    }

    private void UpdateCapability(string name, float value)
    {
        if (!_rows.TryGetValue(name, out var row)) return;
        if (row.LastValue == value)
        {
            return;
        }

        row.LastValue = value;
        row.BarFill.Width = Math.Clamp((int)(value * 60), 0, 60);
        row.BarFill.Background = Brush(GetBarColor(value));
        row.Indicator.Background = Brush(GetStatusColor(value));
    }

    private SolidBrush Brush(Color color)
    {
        if (!_brushes.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(color);
            _brushes[color] = brush;
        }

        return brush;
    }

    private sealed class CapabilityRow
    {
        public readonly Panel BarFill;
        public readonly Panel Indicator;
        public float LastValue;

        public CapabilityRow(Panel barFill, Panel indicator, float lastValue)
        {
            BarFill = barFill;
            Indicator = indicator;
            LastValue = lastValue;
        }
    }
}

