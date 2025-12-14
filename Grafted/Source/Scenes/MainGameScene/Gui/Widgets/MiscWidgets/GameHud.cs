using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class GameHud : HorizontalStackPanel
{
    private readonly Label _bloodLabel;
    private readonly ProgramStatsPanel _programStats;
    private readonly Image _stomachGauge;
    private readonly Panel _stomachContainer;
    private readonly Label _energyLabel;
    private readonly AttackSpeedIcon _attackSpeedLabel;
    private readonly Image _bloodArrow;
    private readonly HorizontalProgressBar _bloodBar;
    private readonly HorizontalProgressBar _energyBar;
    private readonly CheckButton? _pausedCheckBox;
    private readonly Label? _pausedLabel;
    private static readonly Color StatDivider = new(40, 38, 35);
    private static readonly Color AutoStartColor = new(200, 80, 50);

    public GameHud(BaseGui gui, GameContext context)
    {
        var player = context.Player;

        // Main layout
        HorizontalStackPanel leftPanel = new()
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };

        Panel centerPanel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        SetProportionType(centerPanel, ProportionType.Fill);

        // Blood arrow indicator
        _bloodArrow = new Image
        {
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative], Color.DarkGray),
            Width = 24,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Blood percentage label
        _bloodLabel = new Label
        {
            Font = BaseContent.Fonts.Default.Medium,
            Width = 56,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Blood progress bar
        _bloodBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Width = 80,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Value = 100
        };

        // Attack speed display
        _attackSpeedLabel = new AttackSpeedIcon(player.Pawn, BaseContent.Fonts.Default.Medium)
        {
            Height = 44,
            Width = 76,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Energy label
        _energyLabel = new Label
        {
            Font = BaseContent.Fonts.Default.Medium,
            Width = 56,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Energy progress bar
        _energyBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Energy)
        {
            Width = 80,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Value = 100
        };

        // Stomach gauge
        _stomachGauge = new Image
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 32,
            Height = 32,
            Background = new ColoredRegion(new TextureRegion(Defs.BodyParts.Stomach.Icon), Color.White)
        };

        _stomachContainer = new Panel
        {
            Width = 40,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.StomachOutline], Color.White),
            Widgets = { _stomachGauge }
        };

        _programStats = new ProgramStatsPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };

        // === Left Panel Buttons ===
        Button achievements = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Achievements]);
        achievements.TouchDown += (_, _) => { PlayerAchievementsWindow.Toggle(Desktop); };

        Button kills = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull]);
        kills.TouchDown += (_, _) => { PlayerKillsWindow.Toggle(Desktop, context.DeathRecords); };

        Button pawn = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Human], Color.DarkGoldenrod);
        pawn.TouchDown += (_, _) => { gui.ViewEntity(context.PlayerPawn); };

        Button timeline = CreateHudButton(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Combat]);
        timeline.TouchDown += (_, _) => { ZoneTimelineWindow.Toggle(Desktop); };

        leftPanel.Widgets.Add(achievements);
        leftPanel.Widgets.Add(kills);
        leftPanel.Widgets.Add(pawn);
        leftPanel.Widgets.Add(timeline);

        if (gui is CampGui)
        {
            Button nextZone = new(BaseContent.Styles.Button.LargeGold)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = new Label
                {
                    Font = BaseContent.Fonts.Default.Medium,
                    Text = "Begin",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Padding = new Thickness(16, 8)
            };
            nextZone.TouchDown += (_, _) => { (new ZoneSelectionWindow(context.World)).ShowModal(gui.Desktop); };
            leftPanel.Widgets.Add(nextZone);
        }

        // Auto start toggle
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
        
        var pausedPanel = new HorizontalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 100,
            Widgets = { _pausedCheckBox, _pausedLabel }
            
        };
        leftPanel.Widgets.Add(pausedPanel);

        // === Center Stats Panel ===
        var statsContainer = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright],
            Padding = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        if (gui is ZoneGui)
        {
            statsContainer.Margin = new Thickness(80, 0, 0, 0);
        }

        var statsRow = new HorizontalStackPanel
        {
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Blood stat group
        var bloodGroup = CreateStatGroup(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Blood],
            new Color(180, 40, 40),
            _bloodArrow,
            _bloodLabel,
            _bloodBar
        );

        // Attack speed group (center highlight)
        var attackGroup = new Panel
        {
            
            Padding = new Thickness(12, 8),
            VerticalAlignment = VerticalAlignment.Stretch,
            Widgets =
            {
                new HorizontalStackPanel
                {
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        new Image
                        {
                            Width = 36,
                            Height = 36,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.AttackSpeed]
                        },
                        _attackSpeedLabel
                    }
                }
            }
        };

        // Energy stat group
        var energyGroup = CreateStatGroup(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Energy],
            new Color(220, 180, 40),
            null,
            _energyLabel,
            _energyBar
        );

        // Hunger group
        var hungerGroup = new Panel
        {
            Padding = new Thickness(16, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { _stomachContainer }
        };

        // Add dividers between groups
        statsRow.Widgets.Add(bloodGroup);
        statsRow.Widgets.Add(CreateDivider());
        statsRow.Widgets.Add(attackGroup);
        statsRow.Widgets.Add(CreateDivider());
        statsRow.Widgets.Add(energyGroup);
        statsRow.Widgets.Add(CreateDivider());
        statsRow.Widgets.Add(hungerGroup);

        statsContainer.Widgets.Add(statsRow);
        centerPanel.Widgets.Add(statsContainer);

        // === Assemble main layout ===
        Widgets.Add(leftPanel);
        Widgets.Add(centerPanel);
        Widgets.Add(new Panel
        {
            Width = 300,
            Widgets = { _programStats }
        });
    }

    private static Button CreateHudButton(IImage icon, Color? tint = null)
    {
        if (tint != null)
        {
            icon = new ColoredRegion((TextureRegion)icon, tint.Value);
        }
        return new Button(BaseContent.Styles.Button.Dark)
        {
            Content = new Image
            {
                Background = icon,
                Width = 32,
                Height = 32
            },
            Padding = new Thickness(6),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Panel CreateStatGroup(IImage icon, Color iconTint, Image? arrow, Label valueLabel, HorizontalProgressBar bar)
    {
        var iconImage = new Image
        {
            Width = 36,
            Height = 36,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new ColoredRegion((TextureRegion)icon, iconTint)
        };

        var valueStack = new VerticalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { valueLabel, bar }
        };

        var content = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (arrow != null)
        {
            content.Widgets.Add(arrow);
        }
        content.Widgets.Add(iconImage);
        content.Widgets.Add(valueStack);

        return new Panel
        {
            Padding = new Thickness(16, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { content }
        };
    }

    private static Panel CreateDivider()
    {
        return new Panel
        {
            Width = 1,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 6),
            Background = new SolidBrush(StatDivider)
        };
    }

    public void Update()
    {
        Pawn player = Core.Context.PlayerPawn;
        _attackSpeedLabel.Update();
        
        // Sync paused state
        if (_pausedCheckBox != null)
        {
            _pausedCheckBox.IsChecked = !Core.Context.IsPaused;
        }
        if (_pausedLabel != null)
        {
            _pausedLabel.Visible = Core.Context.IsPaused;
        }

        // Blood
        if (player.Body.BloodChangeLastFrame < 0)
        {
            _bloodArrow.Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative], Color.White);
        }
        else
        {
            _bloodArrow.Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative], Color.Transparent);
        }
        float bloodPercent = player.Body.BloodPercent;
        _bloodLabel.Text = $"{Mathf.RoundToInt(bloodPercent * 100)}%";
        _bloodLabel.TextColor = BodyPartColor.GetBloodColor(bloodPercent);
        _bloodBar.Value = bloodPercent * 100;

        // Energy
        float energyPercent = player.Body.EnergyPercent;
        _energyLabel.Text = $"{Mathf.RoundToInt(energyPercent * 100)}%";
        _energyLabel.TextColor = BodyPartColor.GetStomachColor(energyPercent);
        _energyBar.Value = energyPercent * 100;

        // Stomach
        float stomachLevel = player.Body.StomachLevel;
        _stomachGauge.Background = new ColoredRegion(
            Stylesheet.Current.Atlas["stomach-" + Mathf.RoundToInt(Mathf.Lerp(1, 16, stomachLevel))],
            BodyPartColor.GetStomachColor(stomachLevel)
        );
        ((ColoredRegion)_stomachContainer.Background).Color = BodyPartColor.GetStomachColor(stomachLevel);

        _programStats.Update();
    }
}
