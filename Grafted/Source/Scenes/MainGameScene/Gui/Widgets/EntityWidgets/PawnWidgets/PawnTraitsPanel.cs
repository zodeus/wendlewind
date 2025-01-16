namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnTraitsPanel : VerticalStackPanel {
    public PawnTraitsPanel(PawnTraits pawnTraits) {
        Spacing = 5;
        Padding = new Thickness(15);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium) { Text = "Traits" });
        foreach (TraitDef trait in pawnTraits) {
            Widgets.Add(new HorizontalSeparator());
            Widgets.Add(new Label { Text = trait.Label });
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = trait.Description });
        }
    }
}