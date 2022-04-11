using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.CombatGuis;

internal class PawnCombatPanel : HorizontalStackPanel {
    public readonly Pawn Pawn;
    private readonly CombatEvent _combatEvent;
    private PawnBodySummary _bodySummary;
    private Panel _sequencePoints;
    private HorizontalProgressBar _bloodBar;
    private PawnEquipmentPanel _pawnEquipmentPanel;

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
        _pawnEquipmentPanel = new PawnEquipmentPanel(Pawn.Equipment);
        return _pawnEquipmentPanel;
    }

    private Widget GeneratePawnPanel() {
        VerticalStackPanel panel = new() {
            DefaultProportion = Proportion.Auto,
            //ShowGridLines = true
        };
        int panelWidth = 230;

        Image image = new() { Background = new TextureRegion(Pawn.Icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
        image.TouchDown += (_, _) => {
            if (Core.Sim.Gui?.MouseAttachment == null) {
                Core.Sim.Gui!.ViewEntity(Pawn);
            }
        };
        panel.AddChild(image);

        _bloodBar = new HorizontalProgressBar {
            Width = panelWidth, Height = 20,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            VerticalAlignment = VerticalAlignment.Center,
            Filler = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Health],
            Padding = new Thickness(3, 6, 3, 6)
        };
        panel.AddChild(_bloodBar);

        Label namePlate = new() {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12),
            Width = panelWidth - 24 - 32 - 5
        };
        _sequencePoints = new Panel {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark32],
            Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Center,
            Widgets = {
                new Label {
                    Font = BaseContent.Fonts.Fancy.Large,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };
        panel.AddChild(new HorizontalStackPanel { Spacing = 5, Widgets = { namePlate, _sequencePoints } });

        _bodySummary = new PawnBodySummary(Pawn.Body) { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        panel.AddChild(_bodySummary);
        return panel;
    }

    public void Update() {
        if (_combatEvent.State == CombatState.Preparation) {
            return;
        }

        _bloodBar.Value = Pawn.Body.BloodLevel * 100;
        _bodySummary.Update();
        _pawnEquipmentPanel.Update();
        //todo something better here, this is hacky
        int points = _combatEvent.CurrentTurn.PawnTurnData.ContainsKey(Pawn)
            ? _combatEvent.CurrentTurn.PawnTurnData[Pawn].AvailableSequencePoints
            : Mathf.FloorToInt(Pawn.GetStatValue(Defs.Stats.SequencePoints));
        Label label = (Label) _sequencePoints.GetChild(0);
        if (points <= 0) {
            label.TextColor = Color.DarkGray;
        }
        else {
            label.TextColor = Color.YellowGreen;
        }

        label.Text = $"{points}";
    }
}