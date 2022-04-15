using System;
using System.Collections.Generic;
using Grafted.Sim.Entities.Pawns;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Color = Microsoft.Xna.Framework.Color;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnBodyPanel : VerticalStackPanel {
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _scrollBody;
    private readonly PawnSkillsPanel _pawnSkillsPanel;
    private event Action<BodyPartSocket>? _socketClickHandler;

    public PawnBodyPanel(PawnBody body, Action<BodyPartSocket>? socketClickHandler = null) {
        _body = body;
        _socketClickHandler = socketClickHandler;
        _socketPanels = new List<BodyPartSocketPanel>();
        _scrollBody = new VerticalStackPanel { Padding = new Thickness(10), Spacing = 8 };
        _pawnSkillsPanel = new PawnSkillsPanel(_body.Pawn.Skills);
        //Spacing = 5;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(15);
        AddChild(new ScrollViewer {
            Content = new HorizontalStackPanel {
                Spacing = 40,
                Proportions = {
                    Proportion.Fill,
                    Proportion.Auto
                },
                Widgets = {
                    _scrollBody,
                    new VerticalStackPanel {
                        Spacing = 20, Margin = new Thickness(0, 0, 15, 0),
                        Widgets = { new PawnTraitsPanel(_body.Pawn.Traits), _pawnSkillsPanel }
                    }
                }
            }
        });
        GenerateSkeleton();
    }

    private void GenerateSkeleton() {
        _scrollBody.Widgets.Clear();
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

        _scrollBody.AddChild(panel);
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

    public void Update() {
        _pawnSkillsPanel.Update();
        for (int i = _socketPanels.Count - 1; i >= 0; i--) {
            BodyPartSocketPanel socketPanel = _socketPanels[i];

            socketPanel.Update();
            if (socketPanel.Socket.AttachedPart?.IsSevered == true) {
                _socketPanels.RemoveAt(i);
                socketPanel.RemoveFromParent();
            }
        }
    }

    private class BodyPartPanel : HorizontalStackPanel {
        public BodyPart? BodyPart;
        private Label _label;
        private Dictionary<BodyPart, Image> _internalParts = new();

        public BodyPartPanel() {
            Spacing = 5;
            _label = new Label() { VerticalAlignment = VerticalAlignment.Center };
            /*Image image = new() { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), Color.White), Width = 20, Height = 20 };
            _internalParts.Add(bodyPart, image);
            AddChild(image);*/
        }

        public void SetPart(BodyPart bodyPart) {
            _internalParts.Clear();
            Widgets.Clear();

            BodyPart = bodyPart;
            AddChild(_label);
            foreach (BodyPart internalPart in bodyPart.AllInternalParts) {
                Image image = new() { Background = new ColoredRegion(new TextureRegion(internalPart.Icon), BodyPartColor.Get(bodyPart)), Width = 20, Height = 20 };
                _internalParts.Add(internalPart, image);
                AddChild(image);
            }
        }

        public void Update() {
            if (BodyPart == null) {
                return;
            }

            _label.Text = $"{BodyPart.Type}";
            _label.TextColor = BodyPartColor.Get(BodyPart);
            foreach ((BodyPart internalPart, Image image) in _internalParts) {
                ((ColoredRegion) image.Background).Color = BodyPartColor.Get(internalPart);
            }
        }
    }

    private class BodyPartSocketPanel : HorizontalStackPanel {
        public readonly BodyPartSocket Socket;
        private Label _socketLabel;
        private BodyPartPanel _bodyPartPanel;
        private bool _partWasSeveredThisFrame;

        public BodyPartSocketPanel(BodyPartSocket socket) {
            Socket = socket;
            Spacing = 5;
            _socketLabel = new Label() {
                Margin = new Thickness(5, 0, 0, 0),
                Text = $"{socket.Label}",
                VerticalAlignment = VerticalAlignment.Center
            };
            AddChild(_socketLabel);
            _bodyPartPanel = new BodyPartPanel();
            if (Socket.AttachedPart != null) {
                _socketLabel.Visible = false;
                _bodyPartPanel.Visible = true;
                _bodyPartPanel.SetPart(Socket.AttachedPart);
            }

            AddChild(_bodyPartPanel);
        }
        public void Update() {
            if (Socket.AttachedPart == null && _socketLabel.Visible == false) {
                _socketLabel.Visible = true;
                _bodyPartPanel.Visible = false;
            }

            if (Socket.AttachedPart != null && _bodyPartPanel.Visible == false) {
                _socketLabel.Visible = false;
                _bodyPartPanel.Visible = true;
            }

            if (Socket.AttachedPart?.IsSevered == true) {
                _socketLabel.Visible = false;
                _bodyPartPanel.Visible = false;
            }

            _bodyPartPanel.Update();
            _socketLabel.TextColor = BodyPartColor.Get(Socket);
        }
    }
}