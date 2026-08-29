using FontStashSharp.RichText;
namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MapWidgets;

/// <summary>
/// Represents the visual state of a map node.
/// </summary>
public enum MapNodeState
{
    Locked,    // Future zone - dimmed, not clickable
    Current,   // Next available zone - highlighted, clickable
    Completed  // Already beaten - shows checkmark
}

/// <summary>
/// A widget representing a single zone node on the map.
/// Displays the zone's biome color, label, and visual state.
/// </summary>
public class MapNodeWidget : Panel
{
    private readonly Zone _zone;
    private readonly Panel _nodeCircle;

    public const int NodeSize = 80;
    public const int InnerSize = 70;

    private static readonly Color LockedColor = new(50, 50, 55, 200);
    private static readonly Color CurrentBorderColor = new(232, 170, 0); // Gold// Brighter gold
    private static readonly Color CompletedTint = new(120, 120, 120);
    private static readonly Color FurthestEverColor = Color.White;

    public MapNodeState State { get; private set; }
    public bool IsFurthestEver { get; private set; }

    public MapNodeWidget(Zone zone, MapNodeState state, bool isFurthestEver = false)
    {
        _zone = zone;
        State = state;
        IsFurthestEver = isFurthestEver;

        var zoneDef = zone.ZoneDef;

        // Fixed width for consistent grid layout
        Width = 120;

        // Main vertical layout
        var container = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 6
        };

        // Node circle container
        _nodeCircle = new Panel
        {
            Width = NodeSize,
            Height = NodeSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = state == MapNodeState.Current
                ? new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64], CurrentBorderColor)
                : new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64], GetNodeColor())
        };


        if (state == MapNodeState.Completed)
        {
            var stateIcon = new Image
            {
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Checkmark],
                    new Color(100, 220, 100)),
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _nodeCircle.Widgets.Add(stateIcon);
        }
        else
        {
            // Current/Locked: show zone background texture with circular mask
            var tintColor = state == MapNodeState.Locked ? new Color(80, 80, 80) : Color.White;
            // Add dimming overlay for locked zones
            if (state == MapNodeState.Locked)
            {
                var stateIcon = new Image
                {
                    Background = new ColoredRegion(new TextureRegion(_zone.ZoneDef.GetIcon()), new Color(80, 80, 80)),
                    Width = 60,
                    Height = 63,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                _nodeCircle.Widgets.Add(stateIcon);
            }
        }

        // Zone label - centered in a container panel for proper centering
        var labelContainer = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = zoneDef.Label,
            TextColor = GetLabelColor(),
            TextAlign = TextHorizontalAlignment.Center,
            Wrap = true,
        };
        labelContainer.Widgets.Add(label);

        container.Widgets.Add(_nodeCircle);
        container.Widgets.Add(labelContainer);
        Widgets.Add(container);

        // Add "furthest ever reached" indicator (personal best badge)
        // Only show on locked zones to indicate "you've gotten this far before"
        if (isFurthestEver && state == MapNodeState.Locked)
        {
            var furthestBadge = new Panel
            {
                Width = 36 ,
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0),
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Retreat],
                    FurthestEverColor)
            };
            Widgets.Add(furthestBadge);
        }

        // Make interactive if current (clickable, hoverable)
        if (state == MapNodeState.Current)
        {
            TouchDown += OnNodeClicked;
            MouseEntered += OnMouseEntered;
            MouseLeft += OnMouseLeft;
        }
    }

    private void OnMouseEntered(object? sender, EventArgs e)
    {
        // Change cursor to hand
        Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);

        // Brighten the border color
        _nodeCircle.Background = new ColoredRegion(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64],
            Color.GreenYellow);
    }

    private void OnMouseLeft(object? sender, EventArgs e)
    {
        // Restore cursor
        Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);

        // Restore original border color
        _nodeCircle.Background = new ColoredRegion(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64],
            CurrentBorderColor);
    }

    private Color GetNodeColor()
    {
        return State switch
        {
            MapNodeState.Locked => LockedColor,
            MapNodeState.Current => _zone.ZoneDef.ZoneColor,
            MapNodeState.Completed => _zone.ZoneDef.ZoneColor.Multiply(CompletedTint),
            _ => _zone.ZoneDef.ZoneColor
        };
    }

    private Color GetLabelColor()
    {
        return State switch
        {
            MapNodeState.Locked => new Color(90, 90, 90),
            MapNodeState.Current => CurrentBorderColor,
            MapNodeState.Completed => new Color(160, 160, 160),
            _ => Color.White
        };
    }

    private void OnNodeClicked(object? sender, EventArgs e)
    {
        if (State != MapNodeState.Current) return;

        var selectionWindow = new ZoneSelectionWindow(Core.Context.World);
        selectionWindow.ShowModal(Desktop);
    }
}
