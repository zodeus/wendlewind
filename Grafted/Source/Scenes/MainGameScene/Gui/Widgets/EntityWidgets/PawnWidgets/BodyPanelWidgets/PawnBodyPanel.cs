using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPanelWidgets;

public class PawnBodyPanel : VerticalStackPanel {
    private readonly BaseGui _gui;
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _partsPanel;
    private readonly PawnSkillsPanel _pawnSkillsPanel;
    private readonly PawnStatsPanel _pawnStatsPanel;
    private event Action<BodyPartSocket>? _socketClickHandler;

    public PawnBodyPanel(BaseGui gui, PawnBody body, Action<BodyPartSocket>? socketClickHandler = null) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(15);

        _gui = gui;
        _body = body;
        _socketClickHandler = socketClickHandler;
        _socketPanels = new List<BodyPartSocketPanel>();
        _partsPanel = new VerticalStackPanel { Padding = new Thickness(10), Spacing = 3 };
        _pawnSkillsPanel = new PawnSkillsPanel(_body.Pawn.Skills);
        _pawnStatsPanel = new PawnStatsPanel(_body.Pawn);

        AddChild(new ScrollViewer {
            Content = new HorizontalStackPanel {
                Spacing = 40,
                Proportions = {
                    Proportion.Fill,
                    Proportion.Auto
                },
                Widgets = {
                    _partsPanel,
                    new VerticalStackPanel {
                        Spacing = 20, Margin = new Thickness(0, 0, 15, 0),
                        Widgets = { new PawnTraitsPanel(_body.Pawn.Traits), _pawnSkillsPanel,_pawnStatsPanel }
                    }
                }
            }
        });
        GenerateSkeleton();
    }

    private void GenerateSkeleton() {
        _partsPanel.Widgets.Clear();
        _socketPanels.Clear();
        RegisterSocket(_body.RootSocket, 0);
    }

    private void RegisterSocket(BodyPartSocket socket, int padding) {
        BodyPartSocketPanel panel = new(socket) {
            Margin = new Thickness(padding * 25, 0, 0, 0),
        };

        if (_socketClickHandler != null) {
            panel.TouchDown += (_, _) => _socketClickHandler(socket);
        }
        else {
            panel.TouchDown += (_, _) => BodyPartClickHandler(socket);
        }

        _partsPanel.AddChild(panel);
        _socketPanels.Add(panel);

        if (socket.AttachedPart == null) {
            return;
        }

        foreach (BodyPartSocket partSocket in socket.AttachedPart.Sockets) {
            if (partSocket.IsExternal) {
                RegisterSocket(partSocket, padding + 1);
            }
        }
    }

    private void BodyPartClickHandler(BodyPartSocket socket) {
        if (Input.RightMouseButtonReleased) {
            return;
        }

        if (_gui.MouseAttachment == null) {
            //todo ViewSocket's
            if (socket.AttachedPart != null) {
                _gui.ViewEntity(socket.AttachedPart);
            }

            return;
        }

        if (_gui.MouseAttachment.Data is Item item == false) {
            return;
        }

        //Handle Cauterize
        if (socket.AttachedPart == null && socket.IsSealed == false && item.Def == Defs.Items.Cauterize) {
            socket.IsSealed = true;
            return;
        }

        if (socket.AttachedPart is not { } part) {
            return;
        }

        if (item.MedicinalHandler?.ApplyToPart(part) == true) {
            item.StackSize--;
            if (item.StackSize != 0) return;

            item.Destroy();
            _gui.MouseAttachment.Detach();
        }
    }

    public void Update() {
        _pawnSkillsPanel.Update();
        _pawnStatsPanel.Update();
        for (int i = _socketPanels.Count - 1; i >= 0; i--) {
            BodyPartSocketPanel socketPanel = _socketPanels[i];

            socketPanel.Update();
            if (socketPanel.Socket.AttachedPart?.IsSevered == true) {
                _socketPanels.RemoveAt(i);
                socketPanel.RemoveFromParent();
            }
        }
    }

    private class BodyPartRow : HorizontalStackPanel {
        public BodyPart? BodyPart;
        private Label _label;
        private List<ImageCircleIcon> _parts = new();

        public BodyPartRow() {
            Spacing = 5;
            _label = new Label { VerticalAlignment = VerticalAlignment.Center };
            /*Image image = new() { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), Color.White), Width = 20, Height = 20 };
            _internalParts.Add(bodyPart, image);
            AddChild(image);*/
        }

        public void SetPart(BodyPart bodyPart) {
            _parts.Clear();
            Widgets.Clear();

            BodyPart = bodyPart;
            AddChild(_label);

            var parts = bodyPart.AllInternalParts
                .Where(p => p.Type == BodyPartType.Skin)
                .Concat(new List<BodyPart> { bodyPart })
                .Concat(bodyPart.AllInternalParts.Where(p => p.Type != BodyPartType.Skin));
            foreach (BodyPart internalPart in parts) {
                Color defaultColor = new Color(30, 30, 30);
                Color venomColor = new Color(217, 245, 5);
                Color balmColor = new Color(252, 177, 3);
                Image partImage = new() { Background = new ColoredRegion(new TextureRegion(internalPart.WhiteIcon), BodyPartColor.Get(bodyPart)) };
                ImageCircleIcon partIcon = new(partImage, Color.Transparent, panel => {
                    ((ColoredRegion) partImage.Background).Color = BodyPartColor.Get(internalPart);
                    foreach (BodyPartModifier modifier in internalPart.Modifiers) {
                        if (modifier.Def == Defs.BodyPartModifiers.BurningAcid) {
                            ((ColoredRegion) panel.Background).Color = venomColor;
                            return;
                        }

                        if (modifier.Def == Defs.BodyPartModifiers.SoothingBalm) {
                            ((ColoredRegion) panel.Background).Color = balmColor;
                            return;
                        }
                    }

                    ((ColoredRegion) panel.Background).Color = defaultColor;
                });
                _parts.Add(partIcon);
                AddChild(partIcon);
            }
        }

        public void Update() {
            if (BodyPart == null) {
                return;
            }

            _label.Text = $"{BodyPart.Label}";
            _label.TextColor = BodyPartColor.Get(BodyPart);
            foreach (ImageCircleIcon image in _parts) {
                image.Update();
            }
        }
    }

    private class BodyPartSocketPanel : HorizontalStackPanel {
        public readonly BodyPartSocket Socket;
        private Label _socketLabel;
        private BodyPartRow _bodyPartRow;

        public BodyPartSocketPanel(BodyPartSocket socket) {
            Socket = socket;
            Spacing = 5;
            _socketLabel = new Label {
                Margin = new Thickness(5, 0, 0, 0),
                Text = $"{socket.Label}",
                VerticalAlignment = VerticalAlignment.Center
            };
            AddChild(_socketLabel);
            _bodyPartRow = new BodyPartRow();
            if (Socket.AttachedPart != null) {
                _socketLabel.Visible = false;
                _bodyPartRow.Visible = true;
                _bodyPartRow.SetPart(Socket.AttachedPart);
            }

            AddChild(_bodyPartRow);
        }

        public void Update() {
            if (Socket.AttachedPart == null && _socketLabel.Visible == false) {
                _socketLabel.Visible = true;
                _bodyPartRow.Visible = false;
            }

            if (Socket.AttachedPart != null && _bodyPartRow.Visible == false) {
                _socketLabel.Visible = false;
                _bodyPartRow.Visible = true;
            }

            if (Socket.AttachedPart?.IsSevered == true) {
                _socketLabel.Visible = false;
                _bodyPartRow.Visible = false;
            }

            _bodyPartRow.Update();
            _socketLabel.TextColor = BodyPartColor.Get(Socket);
        }
    }
}

public class PawnStatsPanel : VerticalStackPanel {
    public PawnStatsPanel(Pawn pawn) {
        Spacing = 20;
        Padding = new Thickness(15);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        foreach (BaseStat baseStat in pawn.Def.BaseStats) {
            AddChild(new HorizontalStackPanel {
                Widgets = {
                    new Label { Text = baseStat.Def.Label, Width = 250 },
                    new Label { Text = pawn.GetStatValue(baseStat.Def).ToString() }
                }
            });
        }
    }

    public void Update() { }
}