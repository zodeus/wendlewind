using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;

public class SequencePointsIcon : Panel {
    private readonly Pawn _pawn;
    public readonly Label Label;

    public SequencePointsIcon(Pawn pawn) {
        _pawn = pawn;
        Label = new Label {
            Font = BaseContent.Fonts.Fancy.Large,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark32];
        Width = 32;
        Height = 32;
        base.VerticalAlignment = VerticalAlignment.Center;

        AddChild(Label);
    }

    public void Update() {
        if (_pawn.SequencePoints <= 2) {
            Label.TextColor = Color.Orange;
        }
        else {
            Label.TextColor = Color.YellowGreen;
        }

        Label.Text = $"{_pawn.SequencePoints}";
    }
}