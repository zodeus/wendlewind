using FontStashSharp;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class AttackSpeedIcon : Panel
{
    private readonly Pawn _pawn;
    public readonly Label Label;

    public AttackSpeedIcon(Pawn pawn, SpriteFontBase? font = null)
    {
        font ??= BaseContent.Fonts.Fancy.Normal;
        _pawn = pawn;
        Label = new Label
        {
            Font = font,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame];
        Height = 56;
        Padding = new Thickness(6,0,6,0);
        base.VerticalAlignment = VerticalAlignment.Center;

        AddChild(Label);
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

public class ImageCircleIcon : Panel
{
    public readonly Image Image;
    private event Action<Panel>? Handler;

    public ImageCircleIcon(Image image, Color? color = null, Action<Panel>? handler = null)
    {
        Handler = handler;
        Image = image;
        Image.Width = 42;
        Image.Height = 42;
        Image.VerticalAlignment = VerticalAlignment.Center;
        Image.HorizontalAlignment = HorizontalAlignment.Center;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite42], color ?? Color.White);
        Width = 48;
        Height = 48;
        base.VerticalAlignment = VerticalAlignment.Center;

        AddChild(Image);
    }

    public void Update()
    {
        Handler?.Invoke(this);
    }
}