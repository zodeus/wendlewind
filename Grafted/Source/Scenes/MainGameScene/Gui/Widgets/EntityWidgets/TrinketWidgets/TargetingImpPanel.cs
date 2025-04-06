using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class TargetingImpPanel : EntityPanelBase
{
    private readonly TargetingImpHandler? _handler;

    public TargetingImpPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _handler = (TargetingImpHandler)item.TrinketHandler!;
        Padding = new Thickness(20);
        Width = 500;
        Height = 600;
        //var chalice = (item.TrinketHandler as HolyChaliceHandler)!;
        var panel = new HorizontalStackPanel
        {
            Spacing = 15,
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
                        new Label { Text = $"Attacks missed\nwhile targeting" },
                        new Label(BaseContent.Styles.Label.Large)
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextColor = Color.Goldenrod,
                            Text = $"{_handler.AttacksMissed}"
                        }
                    }
                }
            }
        };
        Widgets.Add(panel);
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"Charges {_handler.Charges}/1000", Margin = new Thickness(0, 30, 0, 30)
        });
        Widgets.Add(new VerticalStackPanel()
        {
            Spacing = 10,
            Widgets =
            {
                MakeLevelWidget(1, 10),
                MakeLevelWidget(2, 20),
                MakeLevelWidget(3, 30),
                MakeLevelWidget(4, 40),
                MakeLevelWidget(5, 50),
            }
        });
    }

    private Widget MakeLevelWidget(int p0, int p1)
    {
        return new VerticalStackPanel
        {
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = $"Level {p0}"
                },
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = $"Charges Required {p1}",
                }
            }
        };
    }

    public override void Update()
    {
    }
}