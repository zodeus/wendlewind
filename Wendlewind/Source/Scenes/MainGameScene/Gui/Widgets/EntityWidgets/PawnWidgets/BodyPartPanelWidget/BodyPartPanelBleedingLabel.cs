namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelBleedingLabel : Label
{
    private readonly BodyPart _bodyPart;

    public BodyPartPanelBleedingLabel(BodyPart bodyPart)
    {
        SetStyle(BaseContent.Styles.Label.Normal);
        _bodyPart = bodyPart;
        Padding = new Thickness(12);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        TextColor = new(170, 0, 0);
        Text = "Bleeding";
        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        Visible = _bodyPart.IsBleeding;
    }
}