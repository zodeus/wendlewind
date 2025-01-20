using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnBodyPanel : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _partsPanel;
    private readonly PawnSkillsPanel _pawnSkillsPanel;
    private readonly PawnStatsPanel _pawnStatsPanel;

    public PawnBodyPanel(BaseGui gui, PawnBody body)
    {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(15);

        _gui = gui;
        _body = body;
        _socketPanels = new List<BodyPartSocketPanel>();
        _partsPanel = new VerticalStackPanel { Padding = new Thickness(10), Spacing = 0 };
        _pawnSkillsPanel = new PawnSkillsPanel(_body.Pawn.Skills);
        _pawnStatsPanel = new PawnStatsPanel(_body.Pawn);

        Widgets.Add(new ScrollViewer
        {
            Content = new HorizontalStackPanel
            {
                Spacing = 40,
                Proportions =
                {
                    Proportion.Fill,
                    Proportion.Auto
                },
                Widgets =
                {
                    _partsPanel,
                    // new VerticalStackPanel
                    // {
                    //     Spacing = 20, Margin = new Thickness(0, 0, 15, 0),
                    //     Widgets = { new PawnTraitsPanel(_body.Pawn.Traits), _pawnSkillsPanel, _pawnStatsPanel }
                    // }
                }
            }
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
        BodyPartSocketPanel panel = new(socket, _gui)
        {
            Margin = new Thickness(padding * 25, 0, 0, 0),
        };

        _partsPanel.Widgets.Add(panel);
        _socketPanels.Add(panel);

        if (socket.AttachedPart == null)
        {
            return;
        }

        foreach (BodyPartSocket partSocket in socket.AttachedPart.Sockets)
        {
            if (partSocket.IsExternal)
            {
                RegisterSocket(partSocket, padding + 1);
            }
        }
    }

    public void Update()
    {
        _pawnSkillsPanel.Update();
        _pawnStatsPanel.Update();
        for (int i = _socketPanels.Count - 1; i >= 0; i--)
        {
            BodyPartSocketPanel socketPanel = _socketPanels[i];

            socketPanel.Update();
            if (socketPanel.Socket.AttachedPart?.IsSevered == true)
            {
                _socketPanels.RemoveAt(i);
                socketPanel.RemoveFromParent();
            }
        }
    }

    private class BodyPartRow : HorizontalStackPanel
    {
        private readonly BaseGui _gui;
        public BodyPart? BodyPart;
        private Label _label;
        private List<ImageCircleIcon> _parts = new();

        public BodyPartRow(BaseGui gui)
        {
            _gui = gui;
            Spacing = 5;
            _label = new Label { VerticalAlignment = VerticalAlignment.Center, Font = BaseContent.Fonts.Default.Medium, TextColor = Color.Black };
            /*Image image = new() { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), Color.White), Width = 20, Height = 20 };
            _internalParts.Add(bodyPart, image);
            Widgets.Add(image);*/
        }

        public void SetPart(BodyPart bodyPart)
        {
            _parts.Clear();
            Widgets.Clear();

            BodyPart = bodyPart;
            Widgets.Add(_label);
            _label.TouchDown += (_, _) => BodyPartClickHandler(bodyPart, true);

            var parts = bodyPart.AllInternalParts
                .Where(p => p.Type == BodyPartType.Skin)
                .Concat(new List<BodyPart> { bodyPart })
                .Concat(bodyPart.AllInternalParts.Where(p => p.Type != BodyPartType.Skin));

            Color defaultColor = new Color(30, 30, 30);
            foreach (BodyPart part in parts)
            {
                Image partImage = new() { Background = new ColoredRegion(new TextureRegion(part.WhiteIcon), BodyPartColor.Get(bodyPart)) };
                ImageCircleIcon partIcon = new(partImage, Color.Transparent, panel =>
                {
                    ((ColoredRegion)partImage.Background).Color = BodyPartColor.Get(part);
                    var buffColor = part.Modifiers.Where(m => m.Def.Type == BodyPartModifierType.Buff)
                        .OrderByDescending(m => m.Def.ColorPriority).FirstOrNull()?.Def.Color;
                    var debuffColor = part.Modifiers.Where(m => m.Def.Type == BodyPartModifierType.Debuff)
                        .OrderByDescending(m => m.Def.ColorPriority).FirstOrNull()?.Def.Color;
                    if (buffColor != null || debuffColor != null)
                    {
                        var color = buffColor;
                        if (color != null && debuffColor != null)
                        {
                            color = color.Value.Multiply(debuffColor.Value);
                        }
                        else if (debuffColor != null)
                        {
                            color = debuffColor;
                        }

                        ((ColoredRegion)panel.Background).Color = color!.Value;
                    }
                    else
                    {
                        ((ColoredRegion)panel.Background).Color = defaultColor;
                    }
                });

                partIcon.TouchDown += (_, _) => BodyPartClickHandler(part);
                _parts.Add(partIcon);
                Widgets.Add(partIcon);
            }
        }

        private void BodyPartClickHandler(BodyPart part, bool useItems = false)
        {
            if (Input.RightMouseButtonReleased)
            {
                return;
            }

            if (_gui.MouseAttachment == null)
            {
                _gui.ViewEntity(part);
                return;
            }

            if (useItems && _gui.MouseAttachment.Data is Item item)
            {
                if (item.ItemDef.ItemType == ItemType.Medical && item.MedicinalHandler?.ApplyToPart(item, part) == true)
                {
                    item.StackSize--;
                    _gui.TickGame();
                    if (item.StackSize != 0) return;

                    item.Destroy();
                    _gui.MouseAttachment.Detach();
                }
            }
        }

        public void Update()
        {
            if (BodyPart == null)
            {
                return;
            }

            _label.Text = $"{BodyPart.Label}";
            _label.TextColor = BodyPartColor.Get(BodyPart);
            foreach (ImageCircleIcon image in _parts)
            {
                image.Update();
            }
        }
    }

    private class BodyPartSocketPanel : HorizontalStackPanel
    {
        public readonly BodyPartSocket Socket;
        private readonly BaseGui _gui;
        private Label _socketLabel;
        private BodyPartRow _bodyPartRow;

        public BodyPartSocketPanel(BodyPartSocket socket, BaseGui gui)
        {
            Socket = socket;
            _gui = gui;
            Spacing = 5;
            _socketLabel = new Label
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = $"{socket.Label}",
                VerticalAlignment = VerticalAlignment.Center
            };
            _socketLabel.TouchDown += (_, _) => BodyPartSocketClickHandler(socket);
            Widgets.Add(_socketLabel);
            _bodyPartRow = new BodyPartRow(gui);
            if (Socket.AttachedPart != null)
            {
                _socketLabel.Visible = false;
                _bodyPartRow.Visible = true;
                _bodyPartRow.SetPart(Socket.AttachedPart);
            }

            Widgets.Add(_bodyPartRow);
        }

        private void BodyPartSocketClickHandler(BodyPartSocket socket)
        {
            if (Input.RightMouseButtonReleased)
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
            }

            if (Socket.AttachedPart?.IsSevered == true || (Socket.ParentPart?.IsSevered == true))
            {
                _socketLabel.Visible = false;
                _bodyPartRow.Visible = false;
            }

            _bodyPartRow.Update();
            _socketLabel.TextColor = BodyPartColor.Get(Socket);
        }
    }
}