namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelPartLabel : HorizontalStackPanel
{
    private readonly BodyPart _bodyPart;
    private readonly Label _label;
    private readonly Image _image;

    public BodyPartPanelPartLabel(BaseGui gui, BodyPart bodyPart)
    {
        _bodyPart = bodyPart;
        SetStyle(BaseContent.Styles.Label.Normal);
        Spacing = 10;
        //Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];

        _label = new Label(BaseContent.Styles.Label.Normal) { Text = bodyPart.Label, VerticalAlignment = VerticalAlignment.Center };
        _image = new() { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), Color.White), Width = 32, Height = 32 };
        TouchDown += (_, _) => gui.ViewEntity(bodyPart);
        Widgets.Add(_image);
        Widgets.Add(_label);

        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
        bodyPart.HealthChanged += _ => Refresh();
    }

    private void Refresh()
    {
        ((ColoredRegion)_image.Background).Color = BodyPartColor.Get(_bodyPart);
        _label.TextColor = BodyPartColor.Get(_bodyPart);
    }
}