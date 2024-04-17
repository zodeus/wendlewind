using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Graphics.Textures;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.CombatWidgets;

internal class PawnCombatPanel : HorizontalStackPanel {
    public readonly Pawn Pawn;
    private readonly CombatEvent _combatEvent;
    private PawnBodySummary _bodySummary;
    private SequencePointsIcon _sequencePoints;
    private HorizontalProgressBar _bloodBar;
    private PawnEquipmentPanel _pawnEquipmentPanel;
    private Dictionary<string, Image> _bodyPartImages = new Dictionary<string, Image>();

    public PawnCombatPanel(Pawn pawn, CombatEvent combatEvent, bool equipmentOnLeftSide = true) {
        Pawn = pawn;
        //ShowGridLines = true;
        _combatEvent = combatEvent;

        if (equipmentOnLeftSide) {
            AddChild(GenerateEquipmentPanel());
        }

        AddChild(GeneratePawnPanel());

        if (equipmentOnLeftSide == false) {
            AddChild(GenerateEquipmentPanel());
        }

        Update();
    }

    private Widget GenerateEquipmentPanel() {
        _pawnEquipmentPanel = new PawnEquipmentPanel(Pawn, (part, type) => {
            if (part.Equipment[type] is { } item) {
                if (Pawn.PawnType == PawnType.Player && Input.RightMouseButtonReleased && item.ItemDef.ItemType == ItemType.Potion) {
                    _combatEvent.QueuePotion(item, Pawn);
                    return;
                }

                Core.Sim.Gui!.ViewEntity(item);
            }
        });
        return _pawnEquipmentPanel;
    }

    private Panel InitializeBodyPartImages(int panelWidth) {
        Panel panel = new();

        for (int i = 0; i < Pawn.Body.AllExternalParts.Count; i++) {
            if (Pawn.Body.AllExternalParts[i].Image is not null) {
                BodyPart bodyPart = Pawn.Body.AllExternalParts[i];
                Texture2D icon = bodyPart.Image;
                Image image = new() { Background = new TextureRegion(icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
                image.TouchDown += (_, _) => {
                    if (Core.Sim.Gui?.MouseAttachment == null) {
                        Core.Sim.Gui!.ViewEntity(Pawn);
                    }
                };
                _bodyPartImages.Add(bodyPart.Label, image);
                panel.AddChild(image);
            }
        }

        return panel;
    }

    private void RenderBodyParts() {
        foreach (var bodyPartImage in _bodyPartImages) {
            bodyPartImage.Value.Visible = false;
        }

        foreach (var bodyPart in Pawn.Body.AllExternalParts) {
            if (_bodyPartImages.ContainsKey(bodyPart.Label)) {
                _bodyPartImages[bodyPart.Label].Visible = true;
            }
        }
    }

    private Widget GeneratePawnPanel() {
        VerticalStackPanel panel = new() {
            DefaultProportion = Proportion.Auto,
            //ShowGridLines = true
        };
        int panelWidth = 400;

        if (Pawn.PawnType == PawnType.Enemy || Pawn.Race != Defs.Races.Journeyman) {
            Texture2D icon = Pawn.Icon.Flip(false, true);
            Image image = new() { Background = new TextureRegion(icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
            image.TouchDown += (_, _) => {
                if (Core.Sim.Gui?.MouseAttachment == null) {
                    Core.Sim.Gui!.ViewEntity(Pawn);
                }
            };
            panel.AddChild(image);
        }
        else {
            panel.AddChild(InitializeBodyPartImages(panelWidth));
        }

        _bloodBar = new HorizontalProgressBar {
            Width = panelWidth, Height = 20,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            VerticalAlignment = VerticalAlignment.Center,
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], Pawn.PawnDef.Body.BloodType.Color),
            Padding = new Thickness(3, 6, 3, 6)
        };
        panel.AddChild(_bloodBar);

        Label namePlate = new() {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12),
            Width = panelWidth - 24 - 32 - 5
        };
        _sequencePoints = new SequencePointsIcon(Pawn);
        panel.AddChild(new HorizontalStackPanel { Spacing = 5, Widgets = { namePlate, _sequencePoints } });

        _bodySummary = new PawnBodySummary(Pawn.Body) { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        panel.AddChild(_bodySummary);
        return panel;
    }

    public void Update() {
        if (_combatEvent.State == CombatState.Preparation) {
            return;
        }

        _bloodBar.Value = Pawn.Body.BloodPercent * 100;
        //((ColoredRegion) _bloodBar.Filler).Color = BodyPartColor.GetBloodColor(Pawn.Body.BloodPercent);

        _bodySummary.Update();
        _pawnEquipmentPanel.Update();
        //todo something better here, this is hacky
        int points = _combatEvent.CurrentTurn.PawnTurnData.ContainsKey(Pawn)
            ? _combatEvent.CurrentTurn.PawnTurnData[Pawn].AvailableSequencePoints
            : Pawn.SequencePoints;
        if (points <= 0) {
            _sequencePoints.Label.TextColor = Color.DarkGray;
        }
        else {
            _sequencePoints.Label.TextColor = Color.YellowGreen;
        }

        _sequencePoints.Label.Text = $"{points}";

        RenderBodyParts();
    }
}