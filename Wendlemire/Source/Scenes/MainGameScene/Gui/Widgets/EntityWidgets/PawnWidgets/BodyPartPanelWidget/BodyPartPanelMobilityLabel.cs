﻿namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

internal sealed class BodyPartPanelMobilityLabel : Label
{
    private readonly BodyPart _bodyPart;

    public BodyPartPanelMobilityLabel(BodyPart bodyPart)
    {
        SetStyle(BaseContent.Styles.Label.Normal);
        _bodyPart = bodyPart;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        TextColor = new(170, 0, 0);
        Text = "No Mobility";

        Refresh();
        bodyPart.PartDamaged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        Visible = _bodyPart.HasMobility == false;
    }
}