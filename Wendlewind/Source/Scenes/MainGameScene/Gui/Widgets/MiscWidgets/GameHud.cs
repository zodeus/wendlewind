using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class GameHud : HorizontalStackPanel
{
    private readonly ProgramStatsPanel _programStats;
    private readonly IUpdatable? _centerUpdatable;
    private readonly CheckButton? _pausedCheckBox;
    private readonly Label? _pausedLabel;
    private static readonly Color AutoStartColor = new(200, 80, 50);

    public GameHud(BaseGui gui, GameContext context)
        : this(gui, context, new PawnVitalsPanel(context.PlayerPawn))
    {
    }

    public GameHud(BaseGui gui, GameContext context, Widget? center)
    {
        _centerUpdatable = center as IUpdatable;

        HorizontalStackPanel leftPanel = new()
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Width = 620,
        };

        HorizontalStackPanel rightPanel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _programStats = new ProgramStatsPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };

        CursorButton achievements = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Achievements]);
        achievements.TouchDown += (_, _) => { PlayerAchievementsWindow.Toggle(Desktop); };

        CursorButton kills = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull]);
        kills.TouchDown += (_, _) => { PlayerKillsWindow.Toggle(Desktop, context.DeathRecords); };

        CursorButton pawn = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Human], Color.DarkGoldenrod);
        pawn.TouchDown += (_, _) => { gui.ViewEntity(context.PlayerPawn); };

        leftPanel.Widgets.Add(achievements);
        leftPanel.Widgets.Add(kills);
        leftPanel.Widgets.Add(pawn);

        _pausedCheckBox = new CheckButton
        {
            IsChecked = !context.IsPaused,
            VerticalAlignment = VerticalAlignment.Center
        };
        _pausedCheckBox.Click += (_, _) =>
        {
            context.TogglePause();
        };

        _pausedLabel = new Label
        {
            Font = BaseContent.Fonts.Default.Small,
            Text = "Paused",
            TextColor = AutoStartColor,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = context.IsPaused
        };
        _pausedLabel.TouchDown += (_, _) =>
        {
            _pausedCheckBox.IsChecked = !_pausedCheckBox.IsChecked;
            context.TogglePause();
        };

        var pausedPanel = new VerticalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 100,
            Widgets = { _pausedCheckBox, _pausedLabel }
        };
        leftPanel.Widgets.Add(pausedPanel);

        var achievementsBar = new AchievementBar()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        rightPanel.Widgets.Add(achievementsBar);
        rightPanel.Widgets.Add(_programStats);
        SetProportionType(rightPanel, ProportionType.Fill);
        SetProportionType(achievementsBar, ProportionType.Fill);
        SetProportionType(_programStats, ProportionType.Auto);

        Widgets.Add(leftPanel);
        if (center != null)
        {
            Widgets.Add(center);
        }

        Widgets.Add(rightPanel);
    }

    private static CursorButton CreateHudButton(IImage icon, Color? tint = null)
    {
        if (tint != null)
        {
            icon = new ColoredRegion((TextureRegion)icon, tint.Value);
        }

        return new CursorButton(BaseContent.Styles.Button.Dark)
        {
            Content = new Image
            {
                Background = icon,
                Width = 48,
                Height = 48
            },
            Padding = new Thickness(6),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    public void Update()
    {
        if (_pausedCheckBox != null)
        {
            _pausedCheckBox.IsChecked = !Core.Context.IsPaused;
        }

        if (_pausedLabel != null)
        {
            _pausedLabel.Visible = Core.Context.IsPaused;
        }

        _centerUpdatable?.Update();
        _programStats.Update();
    }
}
