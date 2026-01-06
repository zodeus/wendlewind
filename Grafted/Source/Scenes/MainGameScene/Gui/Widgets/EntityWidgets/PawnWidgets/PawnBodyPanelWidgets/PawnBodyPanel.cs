namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

public sealed class PawnBodyPanel : Panel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _partsPanel;
    private readonly PawnCapabilitiesOverlay _capabilitiesOverlay;
    private readonly PawnInventory? _inventory;
    private readonly MedicalItemsBar? _medicalItemsBar;

    public PawnBodyPanel(BaseGui gui, PawnBody body, PawnInventory? inventory = null)
    {
        MinWidth = 670;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        _gui = gui;
        _body = body;
        _inventory = inventory;
        _socketPanels = new List<BodyPartSocketPanel>();
        Padding = new Thickness(15);

        var mainContainer = new VerticalStackPanel { Spacing = 8 };

        // Medical items bar at the top (if inventory provided)
        if (_inventory != null)
        {
            _medicalItemsBar = new MedicalItemsBar(gui, _inventory)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            mainContainer.Widgets.Add(_medicalItemsBar);
        }

        _partsPanel = new VerticalStackPanel() { Spacing = 2 };

        // Main content - body parts scroll viewer
        mainContainer.Widgets.Add(new ScrollViewer
        {
            Content = _partsPanel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        });

        Widgets.Add(mainContainer);

        // Capabilities overlay in top-right corner
        _capabilitiesOverlay = new PawnCapabilitiesOverlay(body)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 5, 0)
        };
        Widgets.Add(_capabilitiesOverlay);

        MouseLeft += OnMouseLeft;

        GenerateSkeleton();
        Update();
    }

    private void OnMouseLeft(object? sender, EventArgs e)
    {
        // If there's a mouse attachment from the medical bar, detach it
        if (_gui.MouseAttachment?.Data is Item)
        {
            _gui.MouseAttachment.Detach();
        }
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
        _medicalItemsBar?.Update();
    }
}