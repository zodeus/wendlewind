using Myra.Graphics2D.Brushes;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class BoneCrackerPanel : EntityPanelBase
{
    private readonly BoneCrackerHandler? _handler;
    private readonly Label _levelLabel;
    private readonly Label _combatBonesLabel;
    private readonly Label _selfInflictedLabel;
    private readonly Label _totalLabel;
    private readonly HorizontalProgressBar _progressBar;
    private readonly Label _progressLabel;
    private readonly VerticalStackPanel _partsContainer;
    private readonly CursorButton _breakBoneButton;
    private readonly Label _buttonLabel;

    public BoneCrackerPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _handler = item.TrinketHandler as BoneCrackerHandler;
        Padding = new Thickness(24);
        Width = 420;
        Spacing = 0;

        // Header with icon and description
        var header = new HorizontalStackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 16),
            Widgets =
            {
                new Panel
                {
                    Width = 80,
                    Height = 80,
                    Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundElite64],
                    Widgets =
                    {
                        new Image
                        {
                            Background = new TextureRegion(item.GetIcon()),
                            Width = 56,
                            Height = 56,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                new VerticalStackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 4,
                    Widgets =
                    {
                        new Label(BaseContent.Styles.Label.Small)
                        {
                            Text = item.Def.Description,
                            TextColor = new Color(150, 150, 150),
                            Wrap = true,
                            MaxWidth = 280
                        }
                    }
                }
            }
        };
        Widgets.Add(header);

        // Level section with prominent display
        var levelSection = new Panel
        {
            Background = new SolidBrush(new Color(35, 25, 25)),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        _levelLabel = new Label(BaseContent.Styles.Label.Large)
        {
            TextColor = new Color(255, 180, 80),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        levelSection.Widgets.Add(_levelLabel);
        Widgets.Add(levelSection);

        // Progress bar section
        var progressSection = new VerticalStackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 20)
        };
        
        _progressLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(120, 120, 120),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        progressSection.Widgets.Add(_progressLabel);
        
        _progressBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 14,
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], new Color(200, 140, 60))
        };
        progressSection.Widgets.Add(_progressBar);
        Widgets.Add(progressSection);

        // Stats grid
        var statsSection = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 35)),
            Padding = new Thickness(16, 14),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var statsGrid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 10,
            DefaultColumnProportion = Proportion.Auto
        };
        statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        statsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        
        // Combat bones row
        AddStatRow(statsGrid, 0, "Combat Breaks", out _combatBonesLabel, new Color(180, 70, 70));
        
        // Self-inflicted row
        AddStatRow(statsGrid, 1, "Self-Inflicted", out _selfInflictedLabel, new Color(200, 180, 80));
        
        // Total row
        AddStatRow(statsGrid, 2, "Total Broken", out _totalLabel, new Color(160, 160, 160));
        
        statsSection.Widgets.Add(statsGrid);
        Widgets.Add(statsSection);

        // Targetable parts section
        var partsLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "TARGETABLE BODY PARTS",
            TextColor = new Color(100, 100, 100),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Widgets.Add(partsLabel);
        
        _partsContainer = new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Widgets.Add(_partsContainer);

        // Self-inflict button
        _buttonLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Break Your Own Bone",
            TextColor = new Color(200, 80, 80),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        _breakBoneButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = _buttonLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(20, 12)
        };
        _breakBoneButton.Click += OnBreakBoneClicked;
        Widgets.Add(_breakBoneButton);
        
        RefreshDisplay();
    }

    private static void AddStatRow(Grid grid, int row, string labelText, out Label valueLabel, Color valueColor)
    {
        var label = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = labelText,
            TextColor = new Color(100, 100, 100)
        };
        Grid.SetColumn(label, 0);
        Grid.SetRow(label, row);
        grid.Widgets.Add(label);
        
        valueLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            TextColor = valueColor,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueLabel, 1);
        Grid.SetRow(valueLabel, row);
        grid.Widgets.Add(valueLabel);
    }

    private void OnBreakBoneClicked(object? sender, EventArgs e)
    {
        if (_handler == null) return;
        
        var brokenBone = _handler.BreakOwnBone();
        if (brokenBone != null)
        {
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        if (_handler == null)
        {
            _levelLabel.Text = "Level N/A";
            _breakBoneButton.Enabled = false;
            return;
        }

        // Level display
        _levelLabel.Text = $"Level {_handler.Level} / {BoneCrackerHandler.MaxLevel}";

        // Progress bar
        var bonesThisLevel = BoneCrackerHandler.BonesPerLevel - _handler.BonesForNextLevel;
        _progressBar.Value = (float)bonesThisLevel / BoneCrackerHandler.BonesPerLevel * 100;
        _progressLabel.Text = $"{bonesThisLevel} / {BoneCrackerHandler.BonesPerLevel} bones to next level";

        // Stats
        _combatBonesLabel.Text = _handler.CombatBonesBroken.ToString();
        _selfInflictedLabel.Text = _handler.SelfInflictedBonesBroken.ToString();
        _totalLabel.Text = (_handler.CombatBonesBroken + _handler.SelfInflictedBonesBroken).ToString();

        // Update targetable parts display
        RefreshPartsDisplay();

        // Button state
        var playerPawn = Core.Context.PlayerPawn;
        var hasUnbrokenBones = _handler.HasAvailableBones(playerPawn);

        _breakBoneButton.Enabled = hasUnbrokenBones;
        _buttonLabel.Text = hasUnbrokenBones ? "Break Your Own Bone" : "No Targetable Bones Left";
        _buttonLabel.TextColor = hasUnbrokenBones ? new Color(200, 80, 80) : new Color(100, 100, 100);
    }

    private void RefreshPartsDisplay()
    {
        _partsContainer.Widgets.Clear();
        
        if (_handler == null) return;
        
        // Show parts grouped by level, with current and unlocked levels highlighted
        for (var level = 1; level <= BoneCrackerHandler.MaxLevel; level++)
        {
            if (!BoneCrackerHandler.AllowedPartsPerLevel.TryGetValue(level, out var parts)) continue;
            
            var isUnlocked = level <= _handler.Level;
            var isCurrent = level == _handler.Level;
            
            var row = new HorizontalStackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            // Level indicator
            var levelIndicator = new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Lv{level}:",
                TextColor = isUnlocked ? new Color(180, 140, 80) : new Color(70, 70, 70),
                Width = 36
            };
            row.Widgets.Add(levelIndicator);
            
            // Parts list
            var partsText = string.Join(", ", parts.Select(p => p.ToString()));
            var partsListLabel = new Label(BaseContent.Styles.Label.Small)
            {
                Text = partsText,
                TextColor = isUnlocked ? (isCurrent ? new Color(200, 200, 200) : new Color(140, 140, 140)) : new Color(60, 60, 60)
            };
            row.Widgets.Add(partsListLabel);
            
            _partsContainer.Widgets.Add(row);
        }
    }

    public override void Update()
    {
        RefreshDisplay();
    }
}

