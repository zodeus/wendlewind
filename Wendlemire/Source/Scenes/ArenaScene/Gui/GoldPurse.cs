namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class GoldPurse : HorizontalStackPanel
{
    private readonly Label _amount;
    private readonly GameContext _context;

    public GoldPurse(GameContext context)
    {
        _context = context;
        Spacing = 10;
        Padding = new Thickness(14, 8);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];

        Widgets.Add(new Image
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Coin],
            Width = 40,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Center
        });

        _amount = new Label(BaseContent.Styles.Label.Large)
        {
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };
        Widgets.Add(_amount);
        Refresh();
    }

    public void Refresh()
    {
        var gold = _context.ArenaRun?.Gold ?? 0;
        _amount.Text = $"{gold}g";
    }
}
