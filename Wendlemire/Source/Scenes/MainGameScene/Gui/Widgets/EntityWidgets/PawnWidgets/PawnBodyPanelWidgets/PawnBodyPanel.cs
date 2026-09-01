namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

public sealed class PawnBodyPanel : Panel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly HashSet<BodyPartSocket> _registeredSockets = new();
    private readonly VerticalStackPanel _partsPanel;
    private readonly PawnInventory? _inventory;
    private readonly MedicalItemsBar? _medicalItemsBar;
    private readonly bool _hoverToInspect;

    public PawnBodyPanel(BaseGui gui, PawnBody body, PawnInventory? inventory = null, bool fillAvailableHeight = false, bool hoverToInspect = false)
    {
        MinWidth = 536;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        _gui = gui;
        _body = body;
        _inventory = inventory;
        _hoverToInspect = hoverToInspect;
        _socketPanels = new List<BodyPartSocketPanel>();
        Padding = new Thickness(15);

        var mainContainer = new VerticalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

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

        var partsScroll = new ScrollViewer
        {
            Content = _partsPanel,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        mainContainer.Widgets.Add(partsScroll);
        if (fillAvailableHeight)
        {
            VerticalStackPanel.SetProportionType(partsScroll, ProportionType.Fill);
            ClipToBounds = true;
        }

        Widgets.Add(mainContainer);

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
        // Remove all existing widgets
        foreach (var panel in _socketPanels)
        {
            panel.RemoveFromParent();
        }
        _partsPanel.Widgets.Clear();
        _socketPanels.Clear();
        _registeredSockets.Clear();
        RegisterSocket(_body.RootSocket, 0);
    }

    private bool IsMinionSocket(BodyPartSocket socket)
    {
        return socket.Def.AllowedBodyPartTypes.Contains(BodyPartType.Minion);
    }
    
    private void RegisterSocket(BodyPartSocket socket, int padding)
    {
        // Hide empty minion sockets
        if (!ShouldRegisterSocket(socket))
        {
            return;
        }

        BodyPartSocketPanel panel = new(socket, _gui, true, _hoverToInspect)
        {
            Margin = new Thickness(padding * 25, 0, 0, 0),
        };

        _partsPanel.Widgets.Add(panel);
        _socketPanels.Add(panel);
        _registeredSockets.Add(socket);

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
                
                // Also skip sockets that shouldn't be registered (e.g., empty minion sockets)
                if (!ShouldRegisterSocket(appendageSocket)) continue;

                BodyPartSocketPanel p = new(appendageSocket, _gui, false, _hoverToInspect);
                appendagesPanel.Widgets.Add(p);
                _socketPanels.Add(p);
                _registeredSockets.Add(appendageSocket);
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

    private bool ShouldRegisterSocket(BodyPartSocket socket)
    {
        // Empty minion sockets should not be registered (hidden)
        if (socket.AttachedPart == null && IsMinionSocket(socket))
            return false;
        return true;
    }
    
    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        // Check if we need to regenerate the skeleton (e.g., new parts attached to previously-empty sockets)
        bool needsRegeneration = false;
        foreach (var socketPanel in _socketPanels)
        {
            // If a socket now has an attached part with external sockets that we haven't registered yet
            if (socketPanel.Socket.AttachedPart != null)
            {
                foreach (var childSocket in socketPanel.Socket.AttachedPart.Sockets)
                {
                    // Skip sockets that wouldn't be registered anyway
                    if (!ShouldRegisterSocket(childSocket))
                        continue;
                    
                    if (childSocket.IsExternal && !_registeredSockets.Contains(childSocket))
                    {
                        needsRegeneration = true;
                        break;
                    }
                }
            }
            if (needsRegeneration) break;
        }

        if (needsRegeneration)
        {
            GenerateSkeleton();
        }

        for (var i = _socketPanels.Count - 1; i >= 0; i--)
        {
            var socketPanel = _socketPanels[i];

            socketPanel.Update(deltaTime);
            
            // Remove severed parts
            if (socketPanel.Socket.AttachedPart?.IsSevered == true)
            {
                _registeredSockets.Remove(socketPanel.Socket);
                _socketPanels.RemoveAt(i);
                socketPanel.RemoveFromParent();
            }
            // Remove minion sockets that have become empty (minion was removed/died)
            else if (!ShouldRegisterSocket(socketPanel.Socket))
            {
                _registeredSockets.Remove(socketPanel.Socket);
                _socketPanels.RemoveAt(i);
                socketPanel.RemoveFromParent();
            }
        }

        _medicalItemsBar?.Update();
    }

    public void AddDamageText(BodyPart? bodyPart, string text, DynamicSpriteFont font, Color color, float duration = 2f)
    {
        FindRowForPart(bodyPart)?.AddFloater(text, font, color, duration);
    }

    public void FlashPart(BodyPart? bodyPart, Color color)
    {
        FindRowForPart(bodyPart)?.Flash(color);
    }

    private BodyPartRow? FindRowForPart(BodyPart? part)
    {
        if (part == null)
        {
            return null;
        }

        foreach (var panel in _socketPanels)
        {
            var row = panel.PartRow;
            if (row.BodyPart == null)
            {
                continue;
            }

            if (row.BodyPart == part || row.ContainsPart(part))
            {
                return row;
            }
        }

        return null;
    }
}
