namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MapWidgets;

/// <summary>
/// A grid-based map showing all zones as connected nodes.
/// Displays zone progression with 8 nodes per row.
/// </summary>
public class MapPanel : Panel
{
    private readonly World _world;
    private readonly List<MapNodeWidget> _nodeWidgets = new();
    private const int NodesPerRow = 8;
    private static readonly Color ConnectorColor = new(70, 70, 75);
    private static readonly Color ConnectorCompletedColor = new(80, 140, 80);

    public MapPanel(World world)
    {
        _world = world;        
        BuildMap();
    }

    private void BuildMap()
    {
        Widgets.Clear();
        _nodeWidgets.Clear();

        var zones = _world.Zones.OrderBy(z => z.ZoneDef.Stage).ToList();
        bool foundCurrent = false;
        
        // Main container for rows
        var rowsContainer = new VerticalStackPanel
        {
            Spacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Build rows of nodes
        for (int rowStart = 0; rowStart < zones.Count; rowStart += NodesPerRow)
        {
            var rowPanel = new HorizontalStackPanel
            {
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            int rowEnd = Math.Min(rowStart + NodesPerRow, zones.Count);
            
            for (int i = rowStart; i < rowEnd; i++)
            {
                var zone = zones[i];
                var state = DetermineNodeState(zone, ref foundCurrent);
                var isFurthestEver = _world.ProgressTracker.IsFurthestEverReached(zone);

                // Add connector before node (except for first in row)
                if (i > rowStart)
                {
                    var previousZone = zones[i - 1];
                    var connector = CreateConnector(previousZone.IsComplete);
                    rowPanel.Widgets.Add(connector);
                }

                // Add the node
                var nodeWidget = new MapNodeWidget(zone, state, isFurthestEver);
                _nodeWidgets.Add(nodeWidget);
                rowPanel.Widgets.Add(nodeWidget);
            }

            rowsContainer.Widgets.Add(rowPanel);
            
            // Add vertical connector between rows if there's another row
            if (rowStart + NodesPerRow < zones.Count)
            {
                var lastZoneInRow = zones[rowEnd - 1];
                var verticalConnector = CreateVerticalConnector(lastZoneInRow.IsComplete);
                rowsContainer.Widgets.Add(verticalConnector);
            }
        }

        Widgets.Add(rowsContainer);
    }

    private MapNodeState DetermineNodeState(Zone zone, ref bool foundCurrent)
    {
        if (zone.IsComplete)
        {
            return MapNodeState.Completed;
        }

        if (!foundCurrent)
        {
            foundCurrent = true;
            return MapNodeState.Current;
        }

        return MapNodeState.Locked;
    }

    private Widget CreateConnector(bool isCompleted)
    {
        // Horizontal line that visually connects to the node circles
        return new Panel
        {
            Width = 30,
            Height = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30), // Offset to align with node center
            Background = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.White],
                isCompleted ? ConnectorCompletedColor : ConnectorColor)
        };
    }

    private Widget CreateVerticalConnector(bool isCompleted)
    {
        // Vertical connector between rows - positioned at the end of the row
        return new Panel
        {
            Width = 6,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 60, 0), // Align with last node
            Background = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.White],
                isCompleted ? ConnectorCompletedColor : ConnectorColor)
        };
    }
}
