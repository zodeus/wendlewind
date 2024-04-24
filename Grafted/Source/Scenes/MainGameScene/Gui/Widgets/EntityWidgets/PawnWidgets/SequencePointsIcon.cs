namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class SequencePointsIcon : Panel {
    private readonly Pawn _pawn;
    public readonly Label Label;

    public SequencePointsIcon(Pawn pawn) {
        _pawn = pawn;
        Label = new Label {
            Font = BaseContent.Fonts.Fancy.Large,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundDark32];
        Width = 80;
        Height = 80;
        base.VerticalAlignment = VerticalAlignment.Center;

        AddChild(Label);
    }

    public void Update() {
        if (_pawn.MaxSequencePoints <= 2) {
            Label.TextColor = Color.Orange;
        }
        else {
            Label.TextColor = Color.YellowGreen;
        }

        Label.Text = $"{_pawn.MaxSequencePoints}";
    }
}

public class ImageCircleIcon : Panel {
    public readonly Image Image;
    private event Action<Panel>? Handler;

    public ImageCircleIcon(Image image, Color? color = null, Action<Panel>? handler = null) {
        Handler = handler;
        Image = image;
        Image.Width = 22;
        Image.Height = 22;
        Image.VerticalAlignment = VerticalAlignment.Center;
        Image.HorizontalAlignment = HorizontalAlignment.Center;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite28], color ?? Color.White);
        Width = 28;
        Height = 28;
        base.VerticalAlignment = VerticalAlignment.Center;

        AddChild(Image);
    }

    public void Update() {
        Handler?.Invoke(this);
    }
}