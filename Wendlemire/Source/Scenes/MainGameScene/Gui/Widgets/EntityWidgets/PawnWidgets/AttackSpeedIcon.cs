﻿namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class AttackSpeedIcon : Panel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly Label _label;

    public AttackSpeedIcon(Pawn pawn, SpriteFontBase? font = null)
    {
        font ??= BaseContent.Fonts.Default.Normal;
        _pawn = pawn;
        _label = new Label
        {
            Font = font,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame];
        Width = 80;
        Padding = new Thickness(6, 0, 6, 0);
        VerticalAlignment = VerticalAlignment.Center;

        Widgets.Add(_label);
    }

    public void Update()
    {
        if (_pawn.AttackSpeed < 1)
        {
            UiLabel.Set(_label, $"{_pawn.AttackSpeed:.00}", Color.Red);
        }
        else if (_pawn.AttackSpeed < 2)
        {
            UiLabel.Set(_label, $"{_pawn.AttackSpeed:##.0}", Color.Orange);
        }
        else
        {
            UiLabel.Set(_label, $"{_pawn.AttackSpeed:##.0}", Color.YellowGreen);
        }
    }
}