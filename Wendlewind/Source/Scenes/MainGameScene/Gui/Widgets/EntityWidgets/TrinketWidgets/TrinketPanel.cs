namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

public sealed class TrinketPanel : EntityPanelBase
{
    public TrinketPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new Image { Background = item.GetIconImage(), Width = 128, Height = 128 },
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = item.Def.Description, Wrap = true, MaxWidth = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                },
            }
        });
        var kills = item.TrinketHandler?.Kills ?? 0;
        if (kills > 0)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Kills /c[{TC.Golden}]{kills}" });
        }
    }

    public override void Update()
    {
    }
}