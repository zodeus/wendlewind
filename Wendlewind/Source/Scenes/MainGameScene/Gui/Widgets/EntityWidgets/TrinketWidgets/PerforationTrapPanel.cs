using Myra.Graphics2D.Brushes;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

[UsedImplicitly]
public sealed class PerforationTrapPanel : EntityPanelBase
{
    private readonly PerforationTrapHandler _handler;
    private readonly Item _item;
    private readonly ItemUpgradePanel _upgradePanel;

    // Status elements
    private readonly Label _statusLabel;
    private readonly List<Label> _resourceLabels = [];
    private readonly CursorButton _setTrapButton;
    private readonly Label _buttonLabel;

    // Fuse slider (Level 2+)
    private readonly VerticalStackPanel _fuseSliderSection;
    private readonly HorizontalSlider _fuseSlider;
    private readonly Label _fuseValueLabel;

    // Info section
    private readonly Label _durationLabel;

    public PerforationTrapPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        _handler = (PerforationTrapHandler)item.TrinketHandler!;

        var inventory = Core.Context.PlayerPawn.Inventory;
        inventory.ItemAdded += OnInventoryChanged;
        inventory.ItemRemoved += OnInventoryChanged;
        inventory.ItemStackSizeChanged += OnInventoryChanged;

        Padding = new Thickness(24);
        Width = 440;
        Spacing = 0;

        // Header with icon and description
        var header = new HorizontalStackPanel
        {
            Spacing = 16,
            Margin = new Thickness(0, 0, 0, 16),
            Widgets =
            {
                new Image
                {
                    Background = new TextureRegion(item.Icon),
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
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
                            MaxWidth = 300
                        }
                    }
                }
            }
        };
        Widgets.Add(header);

        // Status section
        var statusSection = new Panel
        {
            Background = new SolidBrush(new Color(35, 25, 25)),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 16)
        };

        _statusLabel = new Label(BaseContent.Styles.Label.Large)
        {
            TextColor = new Color(180, 70, 70),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        statusSection.Widgets.Add(_statusLabel);
        Widgets.Add(statusSection);

        // Info section - BloodDrain duration
        var infoSection = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 35)),
            Padding = new Thickness(16, 10),
            Margin = new Thickness(0, 0, 0, 16)
        };

        var infoGrid = new Grid
        {
            ColumnSpacing = 12,
            DefaultColumnProportion = Proportion.Auto
        };
        infoGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        infoGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var durationLabelText = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Duration",
            TextColor = new Color(100, 100, 100)
        };
        Grid.SetColumn(durationLabelText, 0);
        Grid.SetRow(durationLabelText, 0);
        infoGrid.Widgets.Add(durationLabelText);

        _durationLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            TextColor = new Color(200, 80, 80),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(_durationLabel, 1);
        Grid.SetRow(_durationLabel, 0);
        infoGrid.Widgets.Add(_durationLabel);

        infoSection.Widgets.Add(infoGrid);
        Widgets.Add(infoSection);

        // Fuse slider section (Level 2+)
        _fuseSliderSection = new VerticalStackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 16),
            Visible = false
        };

        var sliderRow = new HorizontalStackPanel
        {
            Spacing = 12
        };

        sliderRow.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Fuse Time",
            TextColor = new Color(255, 180, 80),
            VerticalAlignment = VerticalAlignment.Center
        });

        _fuseSlider = new HorizontalSlider
        {
            Width = 140,
            Minimum = 5,
            Maximum = 2000,
            Value = _handler.CustomFuseTime,
            VerticalAlignment = VerticalAlignment.Center
        };
        _fuseSlider.ValueChanged += OnFuseSliderChanged;
        sliderRow.Widgets.Add(_fuseSlider);

        _fuseValueLabel = new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{_handler.CustomFuseTime} ticks",
            Width = 100,
            VerticalAlignment = VerticalAlignment.Center
        };
        sliderRow.Widgets.Add(_fuseValueLabel);

        _fuseSliderSection.Widgets.Add(sliderRow);

        Widgets.Add(_fuseSliderSection);

        // Resource cost display
        var resourceSection = new VerticalStackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 12)
        };

        resourceSection.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Cost to Set:",
            TextColor = new Color(100, 100, 100),
            Margin = new Thickness(0, 0, 0, 4)
        });

        // Create resource rows dynamically from TrapCosts
        foreach (var cost in PerforationTrapHandler.TrapCosts)
        {
            var row = new HorizontalStackPanel { Spacing = 8 };
            row.Widgets.Add(new Image
            {
                Background = new TextureRegion(cost.Item.Texture),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center
            });

            var label = new Label(BaseContent.Styles.Label.Normal)
            {
                TextColor = Color.LightGreen,
                VerticalAlignment = VerticalAlignment.Center
            };
            _resourceLabels.Add(label);
            row.Widgets.Add(label);
            resourceSection.Widgets.Add(row);
        }

        Widgets.Add(resourceSection);

        // Set/Unset button
        _buttonLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Set Trap",
            TextColor = new Color(80, 200, 80),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _setTrapButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = _buttonLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(20, 12),
            Margin = new Thickness(0, 0, 0, 16)
        };
        _setTrapButton.Click += OnSetTrapClicked;
        Widgets.Add(_setTrapButton);

        // Separator
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 0, 0, 12) });

        // Upgrade panel
        _upgradePanel = new ItemUpgradePanel(item, _handler, RefreshDisplay)
        {
            Margin = new Thickness(0, 8, 0, 0)
        };
        Widgets.Add(_upgradePanel);

        RefreshDisplay();
    }

    private void OnFuseSliderChanged(object? sender, EventArgs e)
    {
        _handler.CustomFuseTime = (int)_fuseSlider.Value;
        _fuseValueLabel.Text = $"{_handler.CustomFuseTime} ticks";
    }

    private void OnSetTrapClicked(object? sender, EventArgs e)
    {
        if (_handler.IsSet)
        {
            // Can't unset during combat
            if (Core.Context.CurrentZone?.ActiveEncounter?.State == EncounterState.InProgress)
            {
                return;
            }
            _handler.UnsetTrap();
        }
        else
        {
            _handler.TrySetTrap();
        }

        RefreshDisplay();
        _upgradePanel.Refresh();
    }

    private void OnInventoryChanged(Item _)
    {
        RefreshDisplay();
        _upgradePanel.Refresh();
    }

    private void RefreshDisplay()
    {
        var inventory = Core.Context.PlayerPawn.Inventory;

        // Status
        if (_handler.IsSet)
        {
            if (_handler.FuseTimer > 0)
            {
                _statusLabel.Text = $"ARMED - Fuse: {_handler.FuseTimer}";
                _statusLabel.TextColor = new Color(255, 100, 100);
            }
            else
            {
                _statusLabel.Text = "TRAP SET - Ready";
                _statusLabel.TextColor = new Color(80, 200, 80);
            }
        }
        else
        {
            _statusLabel.Text = "NOT SET";
            _statusLabel.TextColor = new Color(120, 120, 120);
        }

        // Resource display
        for (var i = 0; i < PerforationTrapHandler.TrapCosts.Count; i++)
        {
            var cost = PerforationTrapHandler.TrapCosts[i];
            var available = inventory.AmountOf(cost.Item);
            var hasEnough = available >= cost.Count;

            _resourceLabels[i].Text = $"{cost.Item.Label} x{cost.Count} ({available} available)";
            _resourceLabels[i].TextColor = hasEnough ? Color.LightGreen : Color.IndianRed;
        }

        // Duration info
        var durationSeconds = _handler.BloodDrainDuration / 60f;
        _durationLabel.Text = $"{_handler.BloodDrainDuration} ticks (~{durationSeconds:F1}s)";

        // Fuse slider visibility (Level 1+ - Timed Fuse upgrade)
        _fuseSliderSection.Visible = _handler.UpgradeLevel >= 1;
        _fuseSlider.Value = _handler.CustomFuseTime;
        _fuseValueLabel.Text = $"{_handler.CustomFuseTime} ticks";

        // Button state
        var inCombat = Core.Context.CurrentZone?.ActiveEncounter?.State == EncounterState.InProgress;
        var canSetTrap = _handler.CanSetTrap();

        if (_handler.IsSet)
        {
            _buttonLabel.Text = inCombat ? "Cannot Unset (In Combat)" : "Unset Trap";
            _buttonLabel.TextColor = inCombat ? new Color(100, 100, 100) : new Color(200, 80, 80);
            _setTrapButton.Enabled = !inCombat;
        }
        else
        {
            _buttonLabel.Text = canSetTrap ? "Set Trap" : "Need Resources";
            _buttonLabel.TextColor = canSetTrap ? new Color(80, 200, 80) : new Color(100, 100, 100);
            _setTrapButton.Enabled = canSetTrap;
        }
    }

    public override void Update()
    {
        RefreshDisplay();
    }
}
