using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Combat;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Widgets.TownWidgets;

internal class ZoneBeginWindow : Window {
    private readonly TextButton _begin;
    private readonly Label _timeLabel;
    private readonly TextButton? _travel;

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
        if (zone == Defs.Zones.MeatMarket) {
            _travel = new TextButton(BaseContent.Styles.Button.Large) { Text = $"Travel" };
            _travel.Click += (_, _) => {
                Core.Sim.World.MoveToZone(zone);
                Core.Sim.World.ProgressTime(SimTime.HoursToSeconds(1));
                Core.Sim.ActivateSpecialEvent(zone.Handler, zone.Gui);
            };
            Content = new VerticalStackPanel() {
                Padding = new Thickness(50),
                Background = new ColoredRegion(new TextureRegion(zone.BackgroundTexture), new Color(60, 60, 60, 60)),
                Spacing = 10,
                Widgets = {
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Takes \\c[{UiTextColor.TextColorTime}]{zone.Location.X * 10} minutes \\c[{UiTextColor.TextColorDefault}]to travel" },
                    new HorizontalSeparator(),
                    new Label(BaseContent.Styles.Label.Medium) { Text = "Travel to the Meat Market?" },
                    new HorizontalSeparator(),
                    new HorizontalStackPanel {
                        Spacing = 10, Widgets = {
                            _travel, close
                        }
                    }
                }
            };
        }
        else {
            Content = new VerticalStackPanel() {
                Padding = new Thickness(50),
                Background = new ColoredRegion(new TextureRegion(zone.BackgroundTexture), new Color(20, 20, 20, 20)),
                Spacing = 10,
                Widgets = {
                    _timeLabel,
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Takes \\c[{UiTextColor.TextColorTime}]{zone.Location.X * 10} minutes \\c[{UiTextColor.TextColorDefault}]to travel" },
                    new HorizontalSeparator(),
                    new Label(BaseContent.Styles.Label.Medium) { Text = $"Size: {zone.TravelSize} km" },
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

        if (_travel != null) {
            _travel.Enabled = !Core.Sim.World.Time.IsNight;
        }

        if (Core.Sim.World.Time.IsNight) {
            _timeLabel.Text = $"Current Time \\c[{UiTextColor.TextColorRed}]{Core.Sim.World.Time.CurrentTimeString}";
            _begin.Text = $"\\c[{UiTextColor.TextColorGolden}]Begin";
        }
        else {
            _timeLabel.Text = $"Current Time \\c[{UiTextColor.TextColorGreen}]{Core.Sim.World.Time.CurrentTimeString}";
            _begin.Text = $"\\c[{UiTextColor.TextColorGreen}]Begin";
        }
    }
}