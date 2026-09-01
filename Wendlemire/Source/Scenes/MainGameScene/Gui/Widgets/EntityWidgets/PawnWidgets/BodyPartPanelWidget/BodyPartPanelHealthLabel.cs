﻿namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelHealthLabel : VerticalStackPanel
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

        _image = new Image { Background = new ColoredRegion(new TextureRegion(bodyPart.GetIcon()), Color.White), Width = 128, Height = 128 };
        _label = new Label(BaseContent.Styles.Label.Medium);
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
        if (_bodyPart.HitPoints < 2)
        {
            _label.Text = $"{_bodyPart.HitPoints:N1}/{_bodyPart.MaxHitPoints:N0}";
        }
        else
        {
            _label.Text = $"{Math.Ceiling(_bodyPart.HitPoints):N0}/{_bodyPart.MaxHitPoints:N0}";
        }
    }
}