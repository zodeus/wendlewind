using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnBodyPanel : VerticalStackPanel {
    private readonly PawnBody _body;
    private readonly List<BodyPartSocketPanel> _socketPanels;
    private readonly VerticalStackPanel _partsPanel;
    private readonly PawnSkillsPanel _pawnSkillsPanel;
    private readonly HorizontalProgressBar _bloodBar;
    private event Action<BodyPartSocket>? _socketClickHandler;

    public PawnBodyPanel(PawnBody body, Action<BodyPartSocket>? socketClickHandler = null) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(15);

        _body = body;
        _socketClickHandler = socketClickHandler;
        _socketPanels = new List<BodyPartSocketPanel>();
        _partsPanel = new VerticalStackPanel { Padding = new Thickness(10), Spacing = 8 };
        _pawnSkillsPanel = new PawnSkillsPanel(_body.Pawn.Skills);
        _bloodBar = new HorizontalProgressBar {
            Height = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], Color.White),
            Padding = new Thickness(3, 4, 3, 4)
        };

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
                        Widgets = { new PawnTraitsPanel(_body.Pawn.Traits), _pawnSkillsPanel }
                    }
                }
            }
        });
        GenerateSkeleton();
    }

    private void GenerateSkeleton() {
        _partsPanel.Widgets.Clear();
        _socketPanels.Clear();
        _partsPanel.AddChild(_bloodBar);
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

        if (Core.Sim.Gui!.MouseAttachment == null) {
            if (socket.AttachedPart != null) {
                Core.Sim.Gui!.ViewEntity(socket.AttachedPart);
            }

            return;
        }

        if (Core.Sim.Gui!.MouseAttachment.Data is Item item == false) {
            return;
        }

        //Handle Cauterize
        if (socket.AttachedPart == null && socket.IsSealed == false && item.Def == Defs.Items.Cauterize) {
            socket.IsSealed = true;
            //MouseAttachment.Detach();
            return;
        }

        if (socket.AttachedPart is not { } part) {
            return;
        }

        //Handle MendersMist
        if (item.Def == Defs.Items.MendersMist) {
            item.StackSize--;
            if (item.StackSize == 0) {
                item.Destroy();
                Core.Sim.Gui!.MouseAttachment.Detach();
            }

            float mistJuice = 200;

            float UpdateHealth(BodyPart bodyPart) {
                float currentHealth = bodyPart.HitPoints;
                bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, mistJuice);
                return bodyPart.HitPoints - currentHealth;
            }

            void DoMisting(BodyPart bodyPart) {
                if (mistJuice <= 0) {
                    return;
                }

                mistJuice -= UpdateHealth(bodyPart);
                foreach (BodyPart internalPart in bodyPart.InternalParts) {
                    if (internalPart.IsBone || internalPart.Type is BodyPartType.Skin) {
                        mistJuice -= UpdateHealth(internalPart);
                    }
                }

                foreach (BodyPart externalPart in bodyPart.ExternalParts) {
                    DoMisting(externalPart);
                }
            }

            DoMisting(socket.AttachedPart);
        }

        //Handle MedKit
        if (item.Def == Defs.Items.MedKit) {
            if (part.HealthPercent >= 1) {
                return;
            }

            item.StackSize--;
            if (item.StackSize == 0) {
                item.Destroy();
                Core.Sim.Gui!.MouseAttachment.Detach();
            }

            socket.AttachedPart.HitPoints = socket.AttachedPart.MaxHitPoints;
            foreach (BodyPart internalPart in socket.AttachedPart.InternalParts) {
                internalPart.HitPoints = internalPart.MaxHitPoints;
            }
        }

        //Handle ArterialThreads
        if (item.Def == Defs.Items.ArterialThreads) {
            bool wasConsumed = false;
            foreach (BodyPart internalPart in socket.AttachedPart.InternalParts) {
                if (internalPart.Type == BodyPartType.Artery && internalPart.HealthPercent < 1) {
                    wasConsumed = true;
                    internalPart.HitPoints = internalPart.MaxHitPoints;
                }
            }

            if (wasConsumed) {
                item.StackSize--;
                if (item.StackSize == 0) {
                    item.Destroy();
                    Core.Sim.Gui!.MouseAttachment.Detach();
                }
            }
        }
    }

    public void Update() {
        _pawnSkillsPanel.Update();
        _pawnSkillsPanel.Update();

        _bloodBar.Value = _body.BloodPercent * 100;
        ((ColoredRegion) _bloodBar.Filler).Color = BodyPartColor.GetBloodColor(_body.BloodPercent);

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