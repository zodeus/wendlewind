using Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

public sealed class PawnBodyPanel : Panel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _partsPanel;
    private readonly PawnCapabilitiesOverlay _capabilitiesOverlay;

    public PawnBodyPanel(BaseGui gui, PawnBody body)
    {
        MinWidth = 670;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame], new Color(255, 255, 255, 230));
        _gui = gui;
        _body = body;
        _socketPanels = new List<BodyPartSocketPanel>();
        Padding = new Thickness(15);
        _partsPanel = new VerticalStackPanel() { Spacing = 2 };

        // Main content - body parts scroll viewer
        Widgets.Add(new ScrollViewer
        {
            Content = _partsPanel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        });

        // Capabilities overlay in top-right corner
        _capabilitiesOverlay = new PawnCapabilitiesOverlay(body)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 5, 0)
        };
        Widgets.Add(_capabilitiesOverlay);

        GenerateSkeleton();
    }

    private void GenerateSkeleton()
    {
        _partsPanel.Widgets.Clear();
        _socketPanels.Clear();
        RegisterSocket(_body.RootSocket, 0);
    }

    private void RegisterSocket(BodyPartSocket socket, int padding)
    {
        BodyPartSocketPanel panel = new(socket, _gui, true)
        {
            Margin = new Thickness(padding * 25, 0, 0, 0),
        };

        _partsPanel.Widgets.Add(panel);
        _socketPanels.Add(panel);

        if (socket.AttachedPart == null)
        {
            return;
        }

        if (socket.Def.AllowedBodyPartTypes.Contains(BodyPartType.Hand))
        {
            var appendagesPanel = new HorizontalStackPanel { Margin = new Thickness(padding * 25, 0, 0, 0) };
            _partsPanel.Widgets.Add(appendagesPanel);
            foreach (var appendageSocket in socket.AttachedPart.Sockets)
            {
                if (appendageSocket.IsExternal == false) continue;

                BodyPartSocketPanel p = new(appendageSocket, _gui, false);
                appendagesPanel.Widgets.Add(p);
                _socketPanels.Add(p);
            }
        }
        else
        {
            foreach (var partSocket in socket.AttachedPart.Sockets)
            {
                if (partSocket.IsExternal)
                {
                    RegisterSocket(partSocket, padding + 1);
                }
            }
        }
    }

    public void Update()
    {
        for (var i = _socketPanels.Count - 1; i >= 0; i--)
        {
            var socketPanel = _socketPanels[i];

            socketPanel.Update();
            if (socketPanel.Socket.AttachedPart?.IsSevered == true)
            {
                _socketPanels.RemoveAt(i);
                socketPanel.RemoveFromParent();
            }
        }
        
        _capabilitiesOverlay.Update();
    }
}