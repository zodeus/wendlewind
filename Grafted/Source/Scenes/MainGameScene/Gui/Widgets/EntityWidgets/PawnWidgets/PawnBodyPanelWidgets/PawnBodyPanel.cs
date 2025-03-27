using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

public sealed class PawnBodyPanel : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _partsPanel;

    public PawnBodyPanel(BaseGui gui, PawnBody body)
    {
        //Border = new SolidBrush(Color.Red);
        //BorderThickness = new Thickness(1);
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame], new Color(255, 255, 255, 230));
        _gui = gui;
        _body = body;
        _socketPanels = new List<BodyPartSocketPanel>();
        Padding = new Thickness(15);
        _partsPanel = new VerticalStackPanel
        {
            Spacing = 0, Width = 800
        };

        Widgets.Add(new ScrollViewer
        {
            Content = _partsPanel
        });
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
    }
}