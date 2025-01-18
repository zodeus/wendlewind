using FontStashSharp.RichText;
using Grafted.Scenes.MainGameScene.Gui.CombatGui;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class GameHud : HorizontalStackPanel
{
    private Label _bloodLabel;
    private ProgramStatsPanel _programStats;
    private readonly Label _temperatureLabel;
    private readonly Image _bodyTempIcon;
    private readonly Image _stomachGauge;
    private readonly HorizontalStackPanel _stomachOutline;

    private readonly Label _energyLabel;

    //private MindWidget _mindWidget;
    private readonly AttackSpeedIcon _attackSpeedLabel;
    private readonly Image _bloodArrow;

    public GameHud(BaseGui gui, GameContext context)
    {
        var player = context.Player;
        Spacing = 50;
        HorizontalStackPanel leftPanel = new() { Width = 200, VerticalAlignment = VerticalAlignment.Center };
        HorizontalStackPanel centerPanel = new() { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
        HorizontalStackPanel rightPanel = new() { Width = 300 };
        SetProportionType(leftPanel, ProportionType.Auto);
        SetProportionType(centerPanel, ProportionType.Fill);
        SetProportionType(rightPanel, ProportionType.Auto);
        _bloodArrow = new Image
        {
            Visible = false,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative],
            Width = 48, Height = 48
        };
        _bloodLabel = new Label(BaseContent.Styles.Label.Large)
        {
            Width = 150,
            VerticalAlignment = VerticalAlignment.Center
        };
        _attackSpeedLabel = new AttackSpeedIcon(player.Pawn, BaseContent.Fonts.Fancy.Medium) { Height = 64 };
        _temperatureLabel = new Label(BaseContent.Styles.Label.Large) { VerticalAlignment = VerticalAlignment.Center, Width = 90, TextAlign = TextHorizontalAlignment.Center };
        _energyLabel = new Label(BaseContent.Styles.Label.Medium) { VerticalAlignment = VerticalAlignment.Center, Width = 150, TextAlign = TextHorizontalAlignment.Center };
        _bodyTempIcon = new Image
        {
            VerticalAlignment = VerticalAlignment.Center, Width = 80, Height = 80,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Human], Color.White)
        };
        _stomachGauge = new Image
        {
            VerticalAlignment = VerticalAlignment.Center, Width = 80, Height = 80,
            Background = new ColoredRegion(new TextureRegion(Defs.BodyParts.Stomach.Icon), Color.White)
        };
        _stomachOutline = new HorizontalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Width = 80, Height = 80, Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.StomachOutline], Color.White),
            Widgets =
            {
                _stomachGauge
            }
        };
        //_mindWidget = new MindWidget(Core.Context.World.PlayerPawn);
        _programStats = new ProgramStatsPanel();

        Button kills = new(BaseContent.Styles.Button.Large)
        {
            Content = new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull], Width = 56, Height = 56, },
            Padding = new Thickness(10)
        };
        kills.TouchDown += (_, _) => { new PlayerKillsWindow(context.DeathRecords).Show(Desktop); };
        leftPanel.Widgets.Add(kills);

        Button pawn = new(BaseContent.Styles.Button.Large)
        {
            Content = new Image { Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Human], Color.DarkGoldenrod), Width = 56, Height = 56, },
            Padding = new Thickness(10)
        };
        pawn.TouchDown += (_, _) => { gui.ViewEntity(context.PlayerPawn); };
        leftPanel.Widgets.Add(pawn);

        // Blood
        centerPanel.Widgets.Add(new HorizontalStackPanel
        {
            Widgets =
            {
                new Panel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 48, Height = 48, Widgets = { _bloodArrow }
                },
                new Image
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 80, Height = 80, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Blood]
                },
                _bloodLabel
            }
        });
        // Mind
        // centerPanel.Widgets.Add(new VerticalSeparator());
        // centerPanel.Widgets.Add(_mindWidget);

        // Attack Speed
        centerPanel.Widgets.Add(new Image
        {
            VerticalAlignment = VerticalAlignment.Center,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.AttackSpeed],
            Width = 64, Height = 64
        });
        centerPanel.Widgets.Add(_attackSpeedLabel);

        // Energy
        centerPanel.Widgets.Add(new VerticalStackPanel
        {
            Widgets =
            {
                new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 48, Height = 48, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Energy]
                },
                _energyLabel
            }
        });

        // Hunger
        centerPanel.Widgets.Add(_stomachOutline);

        // // Temperature
        // centerPanel.Widgets.Add(new VerticalSeparator());
        // centerPanel.Widgets.Add(new HorizontalStackPanel {
        //     Widgets = {
        //         new Image {
        //             VerticalAlignment = VerticalAlignment.Center,
        //             Width = 80, Height = 80, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Thermometer]
        //         },
        //         _temperatureLabel,
        //         _bodyTempIcon
        //     }
        // });

        rightPanel.Widgets.Add(_programStats);

        Widgets.Add(leftPanel);
        Widgets.Add(centerPanel);
        Widgets.Add(rightPanel);
    }

    public void Update()
    {
        Pawn player = Core.Context.PlayerPawn;
        _attackSpeedLabel.Update();
        _bloodArrow.Visible = player.Body.BloodChangeLastFrame < 0;
        _bloodLabel.Text = $"{Mathf.RoundToInt(player.Body.BloodPercent * 100)}%";
        _bloodLabel.TextColor = BodyPartColor.GetBloodColor(player.Body.BloodPercent);
        _energyLabel.Text = player.Body.EnergyPercent.ToString("P0");
        _energyLabel.TextColor = BodyPartColor.GetStomachColor(player.Body.EnergyPercent);
        //_mindWidget.Update();
        _temperatureLabel.Text = $"/c[{TC.Blue}]4°C";
        _stomachGauge.Background = new ColoredRegion(
            Stylesheet.Current.Atlas["stomach-" + Mathf.RoundToInt(Mathf.Lerp(1, 16, player.Body.StomachLevel))],
            BodyPartColor.GetStomachColor(player.Body.StomachLevel)
        );
        ((ColoredRegion)_stomachOutline.Background).Color = BodyPartColor.GetStomachColor(player.Body.StomachLevel);

        ((ColoredRegion)_bodyTempIcon.Background).Color = BodyPartColor.GetBodyTemperatureColor(player.Body.Temperature);
        _programStats.Update();
    }
}