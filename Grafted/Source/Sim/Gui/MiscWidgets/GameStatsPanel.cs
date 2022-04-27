using System.Collections.Generic;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.MiscWidgets;

public class GameStatsPanel : HorizontalStackPanel {
    private Label _zoneLabel;
    private Label _zoneKillsLabel;
    private Label _distanceLabel;
    private Label _timeLabel;
    private Label _dayLabel;
    private Label _bloodLabel;

    public GameStatsPanel() {
        Spacing = 10;
        _zoneLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _zoneKillsLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _distanceLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _timeLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _dayLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _bloodLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        AddChild(_zoneLabel);
        AddChild(new VerticalSeparator());
        AddChild(new HorizontalStackPanel {
            Widgets = {
                new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 32, Height = 32, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Blood]
                },
                _bloodLabel
            }
        });
        //todo this is dirty business
        if (Core.Sim.World.CurrentZone.Def.ZoneType == ZoneType.Adventure) {
            AddChild(new VerticalSeparator());
            AddChild(_distanceLabel);
            AddChild(new VerticalSeparator());
            AddChild(_zoneKillsLabel);
        }

        AddChild(new VerticalSeparator());
        AddChild(_timeLabel);
        AddChild(new VerticalSeparator());
        AddChild(_dayLabel);
    }

    public void Update() {
        Pawn player = Core.Sim.World.PlayerPawns[0];
        string plusSign = player.Body.BloodChangeLastFrame > 0 ? "+" : "";
        string bloodLossColor = player.Body.BloodChangeLastFrame switch {
            > 0 => UiTextColor.TextColorGreen,
            < 0 => UiTextColor.TextColorRed,
            _ => UiTextColor.TextColorDefault
        };

        string bloodLoss = $"\\c[{UiTextColor.TextColorDefault}](\\c[{bloodLossColor}]{plusSign}{player.Body.BloodChangeLastFrame:P}/min\\c[{UiTextColor.TextColorDefault}])";
        _bloodLabel.Text = $"{Mathf.RoundToInt(player.Body.BloodPercent * 100)}%{bloodLoss}";
        _bloodLabel.TextColor = BodyPartColor.GetBloodColor(player.Body.BloodPercent);
        _timeLabel.Text = $"{Core.Sim.World.Time}";
        _dayLabel.Text = $"Day {Core.Sim.World.Time.CurrentDayString}";
        _zoneLabel.Text = $"{Core.Sim.World.CurrentZone.Def.Label}";
        _distanceLabel.Text = $"Traveled: {Core.Sim.World.CurrentZone.DistanceTraveled.ToString("0.00")}km ({Mathf.RoundToInt(Core.Sim.World.CurrentZone.PercentTraveled * 100)}%)";
        _zoneKillsLabel.Text = $"Zone Kills: \\c[{UiTextColor.TextColorGreen}]{Core.Sim.World.CurrentZone.ZoneKills}";
    }
}