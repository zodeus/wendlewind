using FontStashSharp;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

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
            _label.TextColor = Color.Red;
            _label.Text = $"{_pawn.AttackSpeed:.00}";
        }
        else if (_pawn.AttackSpeed < 2)
        {
            _label.TextColor = Color.Orange;
            _label.Text = $"{_pawn.AttackSpeed:##.0}";
        }
        else
        {
            _label.TextColor = Color.YellowGreen;
            _label.Text = $"{_pawn.AttackSpeed:##.0}";
        }
    }
}