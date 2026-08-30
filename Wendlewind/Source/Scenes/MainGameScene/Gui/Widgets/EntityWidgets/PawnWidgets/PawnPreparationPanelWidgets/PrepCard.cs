namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public class PrepCard : VerticalStackPanel
{
    protected readonly VerticalStackPanel Body;
    private readonly ScrollViewer _bodyScroll;

    public PrepCard(string title)
    {
        Spacing = 6;
        Padding = new Thickness(8);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];

        Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = title,
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        });
        Widgets.Add(new HorizontalSeparator());

        Body = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _bodyScroll = new ScrollViewer
        {
            Content = Body,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Widgets.Add(_bodyScroll);
        SetProportionType(_bodyScroll, ProportionType.Fill);
    }

    public PrepCard(string title, Widget content) : this(title)
    {
        Body.Widgets.Add(content);
    }

    protected void SetInventory(Widget inventory, int maxHeight = 200)
    {
        var scroll = new ScrollViewer
        {
            Content = inventory,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxHeight = maxHeight
        };
        // Title + separator occupy 0 and 1; keep the item grid under the header.
        Widgets.Insert(2, scroll);
    }

    protected void UseFixedBody()
    {
        _bodyScroll.Content = null;
        Widgets.Remove(_bodyScroll);
        Body.HorizontalAlignment = HorizontalAlignment.Stretch;
        Body.VerticalAlignment = VerticalAlignment.Stretch;
        Widgets.Add(Body);
        SetProportionType(Body, ProportionType.Fill);
    }
}
