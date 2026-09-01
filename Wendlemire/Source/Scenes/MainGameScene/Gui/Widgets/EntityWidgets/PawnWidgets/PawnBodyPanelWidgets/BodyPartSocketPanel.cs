namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartSocketPanel : HorizontalStackPanel
{
    public readonly BodyPartSocket Socket;
    public BodyPartRow PartRow => _bodyPartRow;
    private readonly BaseGui _gui;
    private readonly bool _showInternalParts;
    private readonly bool _hoverToInspect;
    private SocketLabel _socketLabel;
    private BodyPartRow _bodyPartRow;

    public BodyPartSocketPanel(BodyPartSocket socket, BaseGui gui, bool showInternalParts, bool hoverToInspect = false)
    {
        Socket = socket;
        ClipToBounds = false;
        _gui = gui;
        _showInternalParts = showInternalParts;
        _hoverToInspect = hoverToInspect;
        _socketLabel = new SocketLabel(socket, showInternalParts)
        {
            Height = 40
        };
        _socketLabel.TouchDown += (_, _) => BodyPartSocketClickHandler(socket);
        Widgets.Add(_socketLabel);

        _bodyPartRow = new BodyPartRow(gui, hoverToInspect)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Height = 40
        };
        if (Socket.AttachedPart != null)
        {
            _socketLabel.Visible = false;
            _bodyPartRow.Visible = true;
            _bodyPartRow.SetPart(Socket.AttachedPart, showInternalParts);
        }
        else
        {
            // Socket is empty - hide the body part row, show socket label
            _socketLabel.Visible = true;
            _bodyPartRow.Visible = false;
        }

        Widgets.Add(_bodyPartRow);
    }

    private void BodyPartSocketClickHandler(BodyPartSocket socket)
    {
        if (Mouse.GetState().RightButton == ButtonState.Pressed)
        {
            return;
        }

        if (_gui.MouseAttachment?.Data is Item item == false)
        {
            return;
        }

        //Handle Cauterize
        if (socket.AttachedPart == null && socket.IsSealed == false && item.Def == Defs.Items.Cauterize)
        {
            socket.IsSealed = true;
        }
    }

    public void Update()
    {
        Update(1f / 60f);
    }

    public void Update(float deltaTime)
    {
        if (Socket.AttachedPart == null && _socketLabel.Visible == false)
        {
            _socketLabel.Visible = true;
            _bodyPartRow.Visible = false;
        }

        if (Socket.AttachedPart != null && _bodyPartRow.Visible == false)
        {
            _socketLabel.Visible = false;
            _bodyPartRow.Visible = true;
            _bodyPartRow.SetPart(Socket.AttachedPart, _showInternalParts);
        }
        
        // Handle case where the attached part changed (e.g., head regrew)
        if (Socket.AttachedPart != null && _bodyPartRow.BodyPart != Socket.AttachedPart)
        {
            _bodyPartRow.SetPart(Socket.AttachedPart, _showInternalParts);
        }

        if (Socket.AttachedPart?.IsSevered == true || (Socket.ParentPart?.IsSevered == true)) 
        {
            _socketLabel.Visible = false;
            _bodyPartRow.Visible = false;
        }

        _bodyPartRow.Update(deltaTime);
        _socketLabel.Update();
    }
}

internal sealed class SocketLabel : HorizontalStackPanel
{
    private readonly BodyPartSocket _socket;
    private readonly Label _label;
    private readonly BodyPartIcon _icon;


    public SocketLabel(BodyPartSocket socket, bool showInternalParts)
    {
        _socket = socket;
        Spacing = 5;
        _icon = new BodyPartIcon(null);
        _label = new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"{socket.Label}",
            VerticalAlignment = VerticalAlignment.Center
        };
        _label.MouseEntered += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        _label.MouseLeft += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);

        var body = socket.Body ?? socket.ParentPart?.Body;

        if (showInternalParts == false)
        {
            Widgets.Add(_icon);
        }
        else
        {
            Widgets.Add(_label);
        }
    }

    public void Update()
    {
        _label.TextColor = BodyPartColor.Get(_socket);
        _label.TextColor = BodyPartColor.Get(_socket);
        _icon.SetColor(BodyPartColor.Get(_socket));
    }
}