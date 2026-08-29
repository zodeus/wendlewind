namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelBrokenBonesLabel : Label
{
    private readonly BodyPart _bodyPart;

    public BodyPartPanelBrokenBonesLabel(BodyPart bodyPart)
    {
        SetStyle(BaseContent.Styles.Label.Normal);
        _bodyPart = bodyPart;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        TextColor = new(170, 0, 0);
        Text = "Broken Bones";

        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        Visible = _bodyPart.HasBrokenBones;
    }
}internal sealed class BodyPartPanelCrackedLabel : Label
{
    private readonly BodyPart _bodyPart;

    public BodyPartPanelCrackedLabel(BodyPart bodyPart)
    {
        SetStyle(BaseContent.Styles.Label.Normal);
        _bodyPart = bodyPart;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        TextColor = new(170, 0, 0);
        Text = "Cracked";

        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        Visible = _bodyPart.IsCracked;
    }
}