namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class ImageCircleIcon : Panel
{
    private event Action<Panel>? Handler;

    public ImageCircleIcon(Image image, Color? color = null, Action<Panel>? handler = null)
    {
        Handler = handler;
        Padding = new Thickness(8);
        Width = 48;
        Height = 48;
        Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64], color ?? Color.White);
        
        image.VerticalAlignment = VerticalAlignment.Stretch;
        image.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        Widgets.Add(image);
    }

    public void Update()
    {
        Handler?.Invoke(this);
    }
}