namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class ImageCircleIcon : Panel
{
    private readonly ColoredRegion? _imageTexture;
    private readonly ColoredRegion _backgroundTexture;
    private event Action<ImageCircleIcon>? Handler;

    public ImageCircleIcon(ColoredRegion? imageTexture, Action<ImageCircleIcon>? handler = null)
    {
        _imageTexture = imageTexture;
        var image = new Image { Background = imageTexture };
        Handler = handler;
        Padding = new Thickness(8);
        Width = BaseContent.IconSizes.Medium;
        Height = BaseContent.IconSizes.Medium;
        _backgroundTexture = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64], Color.White);
        Background = _backgroundTexture;

        image.VerticalAlignment = VerticalAlignment.Stretch;
        image.HorizontalAlignment = HorizontalAlignment.Stretch;

        Widgets.Add(image);
    }

    public void Update()
    {
        Handler?.Invoke(this);
    }

    public void SetColor(Color color)
    {
        _backgroundTexture.Color = color;
        if (_imageTexture != null)
        {
            _imageTexture.Color = color;
        }
    }
}