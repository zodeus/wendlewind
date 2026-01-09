namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartSocketPanel : HorizontalStackPanel
{
    public readonly BodyPartSocket Socket;
    private readonly BaseGui _gui;
    private readonly bool _showInternalParts;
    private SocketLabel _socketLabel;
    private BodyPartRow _bodyPartRow;

    public BodyPartSocketPanel(BodyPartSocket socket, BaseGui gui, bool showInternalParts)
    {
        Socket = socket;
        _gui = gui;
        _showInternalParts = showInternalParts;
        _socketLabel = new SocketLabel(socket, showInternalParts);
        _socketLabel.TouchDown += (_, _) => BodyPartSocketClickHandler(socket);
        Widgets.Add(_socketLabel);

        _bodyPartRow = new BodyPartRow(gui);
        if (Socket.AttachedPart != null)
        {
            _socketLabel.Visible = false;
            _bodyPartRow.Visible = true;
            _bodyPartRow.SetPart(Socket.AttachedPart, showInternalParts);
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
            _gui.TickGame();
        }
    }

    public void Update()
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

        _bodyPartRow.Update();
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
        if (body?.Pawn.PawnType == PawnType.Enemy)
        {
            Widgets.Add(new Widget
            {
                Width = BaseContent.IconSizes.Small, Height = BaseContent.IconSizes.Medium
            });
        }

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