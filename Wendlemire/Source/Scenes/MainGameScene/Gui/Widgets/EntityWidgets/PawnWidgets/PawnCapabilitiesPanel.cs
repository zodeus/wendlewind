using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// Displays a pawn's capabilities (Sight, Breathing, Mobility, etc.) with visual indicators.
/// </summary>
public sealed class PawnCapabilitiesPanel : VerticalStackPanel, IUpdatable
{
    private readonly PawnCapabilities _capabilities;
    private readonly Dictionary<string, (Label valueLabel, HorizontalProgressBar bar)> _capabilityRows = new();

    // Color palette for capability levels
    private static readonly Color ColorExcellent = new(120, 200, 80);    // Green - 100%
    private static readonly Color ColorGood = new(180, 200, 80);          // Yellow-green - 60-99%
    private static readonly Color ColorWeak = new(220, 160, 60);          // Orange - 30-59%
    private static readonly Color ColorCritical = new(200, 80, 80);       // Red - <30%
    private static readonly Color ColorDisabled = new(100, 95, 90);       // Gray - 0%

    public PawnCapabilitiesPanel(PawnBody body)
    {
        _capabilities = body.Capabilities;
        Spacing = 8;
        Margin = new Thickness(0, 12, 0, 0);

        // Section header with decorative line
        var headerRow = new HorizontalStackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var headerLine = new Panel
        {
            Height = 1,
            Background = new SolidBrush(new Color(80, 70, 55)),
            VerticalAlignment = VerticalAlignment.Center
        };
        HorizontalStackPanel.SetProportionType(headerLine, ProportionType.Fill);
        headerRow.Widgets.Add(headerLine);

        Widgets.Add(headerRow);

        // Container for capability rows
        var capabilitiesContainer = new VerticalStackPanel { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };

        // Add capability rows
        capabilitiesContainer.Widgets.Add(CreateCapabilityRow("Sight", _capabilities.Sight));
        capabilitiesContainer.Widgets.Add(CreateCapabilityRow("Breathing", _capabilities.Breathing));
        capabilitiesContainer.Widgets.Add(CreateCapabilityRow("Circulation", _capabilities.Circulation));
        capabilitiesContainer.Widgets.Add(CreateCapabilityRow("Mobility", _capabilities.Mobility));

        Widgets.Add(capabilitiesContainer);
    }

    private Widget CreateCapabilityRow(string capabilityName, float value)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Status indicator dot
        var indicator = new Panel
        {
            Width = 6,
            Height = 6,
            Background = new SolidBrush(GetStatusColor(value)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0)
        };
        row.Widgets.Add(indicator);

        // Capability name
        var nameLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = capabilityName,
            TextColor = new Color(170, 165, 155),
            Width = 100,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Widgets.Add(nameLabel);

        // Value display
        var valueLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = FormatValue(value),
            TextColor = GetValueColor(value),
            Width = 45,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Widgets.Add(valueLabel);

        // Progress bar for visual representation
        var barContainer = new Panel
        {
            Width = 80,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidBrush(new Color(40, 38, 35)),
            Padding = new Thickness(1)
        };

        var bar = new HorizontalProgressBar
        {
            Height = 6,
            Value = float.IsNaN(value) ? 0 : Math.Clamp(value * 100, 0, 100),
            Filler = new SolidBrush(GetBarColor(value)),
            Background = null
        };
        barContainer.Widgets.Add(bar);
        row.Widgets.Add(barContainer);

        _capabilityRows[capabilityName] = (valueLabel, bar);

        return row;
    }

    private static string FormatValue(float value)
    {
        if (float.IsNaN(value))
            return "n/a";

        return $"{(int)(value * 100)}%";
    }

    private static Color GetStatusColor(float value)
    {
        if (float.IsNaN(value))
            return ColorDisabled;

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
        if (float.IsNaN(value))
            return new Color(100, 95, 90);

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
        if (float.IsNaN(value))
            return new Color(60, 55, 50);

        return value switch
        {
            >= 1f => new Color(100, 170, 70),
            >= 0.6f => new Color(160, 180, 70),
            >= 0.3f => new Color(200, 140, 50),
            > 0f => new Color(180, 70, 70),
            _ => new Color(60, 55, 50)
        };
    }

    public void Update()
    {
        UpdateCapability("Sight", _capabilities.Sight);
        UpdateCapability("Breathing", _capabilities.Breathing);
        UpdateCapability("Mobility", _capabilities.Mobility);
        UpdateCapability("Circulation", _capabilities.Circulation);
    }

    private void UpdateCapability(string name, float value)
    {
        if (!_capabilityRows.TryGetValue(name, out var row))
            return;

        row.valueLabel.Text = FormatValue(value);
        row.valueLabel.TextColor = GetValueColor(value);
        row.bar.Value = float.IsNaN(value) ? 0 : Math.Clamp(value * 100, 0, 100);
        row.bar.Filler = new SolidBrush(GetBarColor(value));
    }
}

