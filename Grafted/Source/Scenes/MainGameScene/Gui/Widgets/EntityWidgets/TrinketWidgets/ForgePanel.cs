namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class ForgePanel : EntityPanelBase
{
    public ForgePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        Width = 500;
        Height = 600;
        //var chalice = (item.TrinketHandler as HolyChaliceHandler)!;
        var panel = new HorizontalStackPanel
        {
            Widgets =
            {
                new Image
                {
                    Background = new TextureRegion(item.Icon),
                    Width = 128, Height = 128
                },
                new VerticalStackPanel
                {
                    Spacing = 10,
                    Widgets =
                    {
                        new Label { Text = "Coming soon..." },
                        new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
                        {
                            //Value = chalice.CurrentOffingPercentage*100,
                            Height = 30,
                            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], new Color(255, 0, 0))
                        }
                    }
                }
            }
        };
        Widgets.Add(panel);
    }

    public override void Update()
    {
    }
}