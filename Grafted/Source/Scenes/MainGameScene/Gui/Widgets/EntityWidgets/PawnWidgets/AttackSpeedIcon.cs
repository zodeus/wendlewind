using FontStashSharp;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class AttackSpeedIcon : Panel, IUpdatable
{
    private readonly Pawn _pawn;
    public readonly Label Label;

    public AttackSpeedIcon(Pawn pawn, SpriteFontBase? font = null)
    {
        font ??= BaseContent.Fonts.Default.Normal;
        _pawn = pawn;
        Label = new Label
        {
            Font = font,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame];
        Height = 56;
        Width = 80;
        Padding = new Thickness(6,0,6,0);
        base.VerticalAlignment = VerticalAlignment.Center;

        Widgets.Add(Label);
    }

    public void Update()
    {
        if (_pawn.AttackSpeed < _pawn.MaxAttackSpeed * .5f)
        {
            Label.TextColor = Color.Red;
        }
        else if (_pawn.AttackSpeed < _pawn.MaxAttackSpeed)
        {
            Label.TextColor = Color.Orange;
        }
        else
        {
            Label.TextColor = Color.YellowGreen;
        }
        Label.Text = $"{_pawn.AttackSpeed:##.0}";
    }
}