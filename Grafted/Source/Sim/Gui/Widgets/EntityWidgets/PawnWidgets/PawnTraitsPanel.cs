using Grafted.Sim.Entities.Pawns;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnTraitsPanel : VerticalStackPanel {
    public PawnTraitsPanel(PawnTraits pawnTraits) {
        Spacing = 5;
        Padding = new Thickness(15);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Traits" });
        foreach (TraitDef trait in pawnTraits) {
            AddChild(new HorizontalSeparator());
            AddChild(new Label { Text = trait.Label });
            AddChild(new Label(BaseContent.Styles.Label.Small) { Text = trait.Description });
        }
    }
}