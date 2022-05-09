using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Combat;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.TownWidgets;

internal class ZoneBeginWindow : Window {
    private readonly TextButton _begin;
    private readonly Label _timeLabel;

    public ZoneBeginWindow(ZoneDef zone) {
        Title = zone.Label;
        _begin = new TextButton(BaseContent.Styles.Button.Large);
        _begin.Click += (_, _) => {
            Core.Sim.World.MoveToZone(zone);
            Core.Sim.World.DoZoneTravel();
            Core.Sim.ActivateCombatEvent(Core.Sim.World.NextCombat());
        };
        _timeLabel = new Label(BaseContent.Styles.Label.Medium);
        TextButton close = new(BaseContent.Styles.Button.Large) { Text = $"\\c[{UiTextColor.TextColorRed}]Cancel" };
        close.Click += (_, _) => Close();
        Content = new VerticalStackPanel() {
            Padding = new Thickness(50),
            Spacing = 10,
            Widgets = {
                new Label(BaseContent.Styles.Label.Medium) { Text = $"Size: {zone.TravelSize} km" },
                _timeLabel,
                new HorizontalSeparator(),
                new Label(BaseContent.Styles.Label.Medium) { Text = "Enemies" },
                GenerateZoneEnemies(zone),
                new HorizontalSeparator(),
                new Label(BaseContent.Styles.Label.Medium) { Text = "Resources" },
                GenerateZoneResources(zone),
                new HorizontalSeparator(),
                new HorizontalStackPanel {
                    Spacing = 10, Widgets = {
                        _begin, close
                    }
                }
            }
        };
    }

    private Widget GenerateZoneEnemies(ZoneDef zone) {
        HorizontalStackPanel panel = new() { Spacing = 5 };
        var enemies = DefRepository<CombatConfigDef>.Defs.Where(def => def.Zone == zone).SelectMany(def => def.Enemies).DistinctBy(record => record.Race);
        foreach (CombatConfigEnemyRecord enemyConfig in enemies) {
            panel.AddChild(new Image() { Background = new TextureRegion(enemyConfig.Race.Icon), Width = 64, Height = 64 });
        }

        return panel;
    }

    private Widget GenerateZoneResources(ZoneDef zone) {
        HorizontalStackPanel panel = new() { Spacing = 5 };
        foreach (ZoneResourceRecord record in zone.Resources) {
            panel.AddChild(new Image() { Background = new TextureRegion(record.Item.Icon), Width = 32, Height = 32 });
        }

        return panel;
    }

    public void Update() {
        if (!IsPlaced) {
            return;
        }

        if (Core.Sim.World.Time.IsNight) {
            _timeLabel.Text = $"\\c[{UiTextColor.TextColorRed}]{Core.Sim.World.Time.CurrentTimeString}";
            _begin.Text = $"\\c[{UiTextColor.TextColorGolden}]Begin";
        }
        else {
            _timeLabel.Text = $"\\c[{UiTextColor.TextColorGreen}]{Core.Sim.World.Time.CurrentTimeString}";
            _begin.Text = $"\\c[{UiTextColor.TextColorGreen}]Begin";
        }
    }
}