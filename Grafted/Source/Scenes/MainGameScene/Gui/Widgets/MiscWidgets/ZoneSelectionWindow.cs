namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class ZoneSelectionWindow : Window
{
    private readonly VerticalStackPanel _zoneDisplay;

    public sealed override Widget Content
    {
        get => base.Content;
        set => base.Content = value;
    }

    public ZoneSelectionWindow(World world)
    {
        TitlePanel.Visible = false;
        _zoneDisplay = new VerticalStackPanel();
        var zoneButtonPanel = new VerticalStackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 10, };

        Zone? previousZone = null;
        foreach (var zone in world.Zones)
        {
            ShouldShowZone(zone, previousZone);
            previousZone = zone;
        }

        // The Alchemist Hut
        // Forgemaster's Quarry
        // Fallow Field
        // Mage Tower
        // Field of Vegetables
        // Blood Court
        // His Rectory
        // Scarlet Chapel
        // Steamy Oil Vents            

        Content = new HorizontalStackPanel { Widgets = { zoneButtonPanel, _zoneDisplay } };
    }

    private void ShouldShowZone(Zone zone, Zone? previousZone = null)
    {
        if (zone.IsComplete || previousZone is { IsComplete: false }) return;

        _zoneDisplay.Widgets.Clear();
        var startButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = $"/c[{TC.Green}]Start" }
        };
        startButton.Click += (_, _) =>
        {
            Core.Context.Save();
            Core.Context.EnterZone(zone.ZoneDef);
        };
        Button close = new(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = $"/c[{TC.Red}]Cancel" }
        };
        close.Click += (_, _) => Close();
        _zoneDisplay.Widgets.Add(new VerticalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(40, 0, 40, 0),
            Background = new ColoredRegion(new TextureRegion(zone.ZoneDef.BackgroundTexture), new Color(20, 20, 20, 20)),
            Spacing = 5,
            Widgets =
            {
                new ZoneDetailsPanel(zone),
                new HorizontalStackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20),
                    Spacing = 10, Widgets =
                    {
                        startButton, close
                    }
                }
            }
        });
    }
}
