namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelHealthLabel : HorizontalStackPanel
{
    private readonly BodyPart _bodyPart;
    private readonly Label _label;
    private readonly Image _image;

    public BodyPartPanelHealthLabel(BodyPart bodyPart)
    {
        _bodyPart = bodyPart;
        SetStyle(BaseContent.Styles.Label.Large);
        Margin = new Thickness(0, 0, 0, 10);
        Spacing = 10;
        
        _image = new Image { Background = new ColoredRegion(new TextureRegion(bodyPart.Icon), Color.White), Width = 48, Height = 48 };
        _label = new Label(BaseContent.Styles.Label.Large);
        Widgets.Add(_image);
        Widgets.Add(_label);


        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        ((ColoredRegion)_image.Background).Color = BodyPartColor.Get(_bodyPart);
        _label.TextColor = BodyPartColor.Get(_bodyPart);
        _label.Text = $"{_bodyPart.HitPoints}/{_bodyPart.MaxHitPoints} {_bodyPart.HealthPercent:P0}";
    }
}