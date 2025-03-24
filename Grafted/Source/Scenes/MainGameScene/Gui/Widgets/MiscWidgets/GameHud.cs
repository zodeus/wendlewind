using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public class GameHud : HorizontalStackPanel
{
    private Label _bloodLabel;
    private ProgramStatsPanel _programStats;
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
        HorizontalStackPanel leftPanel = new() { Spacing = 10, Width = 600, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
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
        _bloodLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Width = 100,
            VerticalAlignment = VerticalAlignment.Center
        };
        _attackSpeedLabel = new AttackSpeedIcon(player.Pawn, BaseContent.Fonts.Default.Medium) { Height = BaseContent.IconSizes.Medium };
        _energyLabel = new Label(BaseContent.Styles.Label.Medium)
        {
            Width = 100,
            VerticalAlignment = VerticalAlignment.Center
        };
        _stomachGauge = new Image
        {
            VerticalAlignment = VerticalAlignment.Center, Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium,
            Background = new ColoredRegion(new TextureRegion(Defs.BodyParts.Stomach.Icon), Color.White)
        };
        _stomachOutline = new HorizontalStackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.StomachOutline], Color.White),
            Widgets =
            {
                _stomachGauge
            }
        };
        //_mindWidget = new MindWidget(Core.Context.World.PlayerPawn);
        _programStats = new ProgramStatsPanel();
        Button achievements = new(BaseContent.Styles.Button.Large)
        {
            Content = new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Achievements], Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium, },
            Padding = new Thickness(10)
        };
        achievements.TouchDown += (_, _) => { new PlayerAchievementsWindow().Show(Desktop); };
        leftPanel.Widgets.Add(achievements);

        Button kills = new(BaseContent.Styles.Button.Large)
        {
            Content = new Image { Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull], Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium, },
            Padding = new Thickness(10)
        };
        kills.TouchDown += (_, _) => { new PlayerKillsWindow(context.DeathRecords).Show(Desktop); };
        leftPanel.Widgets.Add(kills);

        Button pawn = new(BaseContent.Styles.Button.Large)
        {
            Content = new Image
            {
                Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Human], Color.DarkGoldenrod), Width = BaseContent.IconSizes.Medium,
                Height = BaseContent.IconSizes.Medium,
            },
            Padding = new Thickness(10)
        };
        pawn.TouchDown += (_, _) => { gui.ViewEntity(context.PlayerPawn); };
        leftPanel.Widgets.Add(pawn);

        Button boak = new(BaseContent.Styles.Button.Large)
        {
            Content = new Image
            {
                Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Boak], Color.White), Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium,
            },
            Padding = new Thickness(10)
        };
        boak.TouchDown += (_, _) => { gui.OpenBoak(); };
        leftPanel.Widgets.Add(boak);

        if (context.CurrentZone == null)
        {
            Button nextZone = new(BaseContent.Styles.Button.Large)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = new Label(BaseContent.Styles.Label.Large)
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "Carry On"
                },
                Padding = new Thickness(10)
            };
            nextZone.TouchDown += (_, _) => { (new ZoneSelectionWindow(context.World)).ShowModal(gui.Desktop); };
            leftPanel.Widgets.Add(nextZone);
        }


        // Blood
        centerPanel.Widgets.Add(new HorizontalStackPanel
        {
            Widgets =
            {
                new Panel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = BaseContent.IconSizes.Default, Height = BaseContent.IconSizes.Default, Widgets = { _bloodArrow }
                },
                new Image
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = BaseContent.IconSizes.Large, Height = BaseContent.IconSizes.Large, Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Blood]
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
            Width = BaseContent.IconSizes.Medium, Height = BaseContent.IconSizes.Medium
        });
        centerPanel.Widgets.Add(_attackSpeedLabel);

        // Energy
        centerPanel.Widgets.Add(new HorizontalStackPanel()
        {
            Spacing = 0,
            Widgets =
            {
                new Image
                {
                    Width = BaseContent.IconSizes.Large,
                    Height = BaseContent.IconSizes.Large,
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Energy]
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
        _stomachGauge.Background = new ColoredRegion(
            Stylesheet.Current.Atlas["stomach-" + Mathf.RoundToInt(Mathf.Lerp(1, 16, player.Body.StomachLevel))],
            BodyPartColor.GetStomachColor(player.Body.StomachLevel)
        );
        ((ColoredRegion)_stomachOutline.Background).Color = BodyPartColor.GetStomachColor(player.Body.StomachLevel);
        _programStats.Update();
    }
}