using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class MindWidget : VerticalStackPanel {
    private readonly Pawn _pawn;
    private readonly HorizontalProgressBar _sanity;
    private readonly HorizontalProgressBar _power;
    private readonly HorizontalProgressBar _focus;

    public MindWidget(Pawn pawn) {
        _pawn = pawn;
        Spacing = 1;

        _sanity = new HorizontalProgressBar {
            Width = 40, Height = 6,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], new Color(252, 86, 225)),
            Padding = new Thickness(3, 3, 3, 3),

            Value = 100f
        };

        _power = new HorizontalProgressBar {
            Width = 40, Height = 6,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], new Color(20, 189, 255)),
            Padding = new Thickness(3, 3, 3, 3),
            Value = 50f
        };

        _focus = new HorizontalProgressBar() {
            Width = 40, Height = 6,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], new Color(20, 255, 138)),
            Padding = new Thickness(3, 3, 3, 3),

            Value = 70f
        };
        AddChild(new HorizontalStackPanel { Spacing = 5, Widgets = { new Label(BaseContent.Styles.Label.Small) { Text = "S" }, _sanity } });
        AddChild(new HorizontalStackPanel { Spacing = 5, Widgets = { new Label(BaseContent.Styles.Label.Small) { Text = "P" }, _power } });
        AddChild(new HorizontalStackPanel { Spacing = 5, Widgets = { new Label(BaseContent.Styles.Label.Small) { Text = "F" }, _focus } });
    }

    public void Update() {
        _sanity.Value = _pawn.Mind.Sanity * 100;
        _power.Value = _pawn.Mind.Power * 100;
        _focus.Value = _pawn.Mind.Focus * 100;
    }
}

public class GameHud : HorizontalStackPanel {
    private Label _zoneLabel;
    private Label _bloodLabel;
    private ProgramStatsPanel _programStats;
    private readonly Label _temperatureLabel;
    private readonly Image _bodyTempIcon;
    private readonly Image _stomachGauge;
    private readonly HorizontalStackPanel _stomachOutline;
    private readonly Label _energyLabel;
    private readonly SequencePointsIcon _sequencePoints;
    private MindWidget _mindWidget;

    public GameHud(Player player) {
        Spacing = 50;
        HorizontalStackPanel leftPanel = new() { Width = 200 };
        HorizontalStackPanel centerPanel = new() { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
        HorizontalStackPanel rightPanel = new() { Width = 200 };
        Proportions.Add(Proportion.Auto);
        Proportions.Add(Proportion.Fill);
        Proportions.Add(Proportion.Auto);
        _zoneLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _bloodLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center };
        _temperatureLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center, Width = 90, TextAlign = TextAlign.Center };
        _energyLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center, Width = 150, TextAlign = TextAlign.Center };
        _sequencePoints = new SequencePointsIcon(player.Pawn);
        _bodyTempIcon = new Image {
            VerticalAlignment = VerticalAlignment.Center, Width = 80, Height = 80,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Human], Color.White)
        };
        _stomachGauge = new Image {
            VerticalAlignment = VerticalAlignment.Center, Width = 80, Height = 80,
            Background = new ColoredRegion(new TextureRegion(Defs.BodyParts.Stomach.Icon), Color.White)
        };
        _stomachOutline = new HorizontalStackPanel {
            VerticalAlignment = VerticalAlignment.Center,
            Width = 80, Height = 80, Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.StomachOutline], Color.White),
            Widgets = {
                _stomachGauge
            }
        };
        _mindWidget = new MindWidget(Core.Context.World.PlayerPawn);
        _programStats = new ProgramStatsPanel();
        centerPanel.AddChild(_zoneLabel);

        // Blood
        centerPanel.AddChild(new VerticalSeparator());
        centerPanel.AddChild(new HorizontalStackPanel {
            Widgets = {
                new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 80, Height = 80, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Blood]
                },
                _bloodLabel
            }
        });
        // Mind
        centerPanel.AddChild(new VerticalSeparator());
        centerPanel.AddChild(_mindWidget);

        // Energy
        centerPanel.AddChild(new VerticalSeparator());
        centerPanel.AddChild(new HorizontalStackPanel {
            Widgets = {
                new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 80, Height = 80, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Energy]
                },
                _energyLabel
            }
        });

        // Hunger
        centerPanel.AddChild(new VerticalSeparator());
        centerPanel.AddChild(_stomachOutline);
        // Hunger
        centerPanel.AddChild(new VerticalSeparator());
        centerPanel.AddChild(_sequencePoints);

        // Temperature
        centerPanel.AddChild(new VerticalSeparator());
        centerPanel.AddChild(new HorizontalStackPanel {
            Widgets = {
                new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 80, Height = 80, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Thermometer]
                },
                _temperatureLabel,
                _bodyTempIcon
            }
        });

        rightPanel.AddChild(_programStats);

        AddChild(leftPanel);
        AddChild(centerPanel);
        AddChild(rightPanel);
    }

    public void Update() {
        Pawn player = Core.Context.PlayerPawn;
        string plusSign = player.Body.BloodChangeLastFrame > 0 ? "+" : "";
        string bloodLossColor = player.Body.BloodChangeLastFrame switch {
            > 0 => TC.Green,
            < 0 => TC.Red,
            _ => TC.Default
        };

        string bloodLoss = $"\\c[{TC.Default}] (\\c[{bloodLossColor}]{plusSign}{player.Body.BloodChangeLastFrame:P}/min\\c[{TC.Default}])";
        _bloodLabel.Text = $"{Mathf.RoundToInt(player.Body.BloodPercent * 100)}%{bloodLoss}";
        _bloodLabel.TextColor = BodyPartColor.GetBloodColor(player.Body.BloodPercent);
        _zoneLabel.Text = $"blah";
        //_zoneLabel.Text = $"{Core.Context.World.CurrentZone?.Def.Label}";
        _sequencePoints.Update();
        _energyLabel.Text = player.Body.Energy.ToString("P0");
        _energyLabel.TextColor = BodyPartColor.GetStomachColor(player.Body.Energy);
        _mindWidget.Update();
        _temperatureLabel.Text = $"\\c[{TC.Blue}]4°C";
        _stomachGauge.Background = new ColoredRegion(
            Stylesheet.Current.Atlas["stomach-" + Mathf.RoundToInt(Mathf.Lerp(1, 16, player.Body.StomachLevel))],
            BodyPartColor.GetStomachColor(player.Body.StomachLevel)
        );
        ((ColoredRegion) _stomachOutline.Background).Color = BodyPartColor.GetStomachColor(player.Body.StomachLevel);

        ((ColoredRegion) _bodyTempIcon.Background).Color = BodyPartColor.GetBodyTemperatureColor(player.Body.Temperature);
        _programStats.Update();
    }
}