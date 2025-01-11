namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelFunctionalLabel : Label
{
    private readonly BodyPart _bodyPart;

    public BodyPartPanelFunctionalLabel(BodyPart bodyPart)
    {
        SetStyle(BaseContent.Styles.Label.Normal);
        _bodyPart = bodyPart;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        TextColor = new(170, 0, 0);
        Text = "Non-functional";

        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        Visible = _bodyPart.IsFunctional == false;
    }
}