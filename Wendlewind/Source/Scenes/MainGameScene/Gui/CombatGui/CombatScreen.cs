using Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using TestSimLauncher = Wendlewind.Scenes.MainGameScene.TestSimLauncher;
using TestSimSettings = Wendlewind.Scenes.MainGameScene.TestSimSettings;

namespace Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

public class CombatScreen : VerticalStackPanel, IDisposable
{
    private readonly ZoneGui _gui;
    private readonly GameContext _context;
    private readonly ScrollViewer _combatLog;
    private readonly GameHud _gameHud;
    private readonly CombatPartyPanel _playerPartyPanel;
    private readonly CombatPartyPanel _opponentPartyPanel;
    private readonly CombatControlPanel _controlPanel;
    private readonly PawnBodyPanel _pawnBodyView;
    private readonly PawnBodyPanel _enemyPawnBodyView;
    private readonly Label _tickLabel;
    private Window? _combatLogWindow;
    private CombatSummaryWindow? _summaryWindow;
    private CursorButton? _showSummaryButton;

    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatScreen(ZoneGui gui, GameContext context)
    {
        _gui = gui;
        _context = context;
        Encounter.StateChangedAction += CombatStateChangedAction;
        Encounter.CombatHandler!.CombatLogMessageAdded += AddCombatLogEntry;
        Encounter.CombatHandler!.EventOccured += PrintDamage;
        Margin = new Thickness(0, 5, 0, 0);
        _gameHud = new GameHud(gui, context)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        _playerPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.PlayerPawns, HorizontalAlignment.Right);
        Grid.SetRow(_playerPartyPanel, 1);
        Grid.SetColumn(_playerPartyPanel, 0);

        _opponentPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.EnemyPawns, HorizontalAlignment.Left);
        Grid.SetRow(_opponentPartyPanel, 1);
        Grid.SetColumn(_opponentPartyPanel, 2);

        _controlPanel = new CombatControlPanel(Encounter)
        {
            MinWidth = 190,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetRow(_controlPanel, 2);
        Grid.SetColumn(_controlPanel, 0);

        _pawnBodyView = new PawnBodyPanel(gui, Encounter.PlayerPawns.First().Body)
        {
            Height = 1300
        };

        _enemyPawnBodyView = new PawnBodyPanel(gui, Encounter.EnemyPawns.First().Body)
        {
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _combatLog = new ScrollViewer
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 1450,
            Height = 620,
            Content = new VerticalStackPanel { Padding = new Thickness(0), Spacing = 12 }
        };

        _tickLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Margin = new Thickness(0, 15, 0, 15),
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "0",
            TextColor = Color.DarkGoldenrod
        };

        var logsButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Logs"
            }
        };
        var rematchButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = DebugSettings.TestSimMode,
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Rematch" }
        };
        rematchButton.Click += (_, _) => TestSimLauncher.Rematch(_context);

        var rerollButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = DebugSettings.TestSimMode,
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Reroll" }
        };
        rerollButton.Click += (_, _) => TestSimLauncher.Reroll(_context);

        var swapButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = DebugSettings.TestSimMode,
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Swap" }
        };
        swapButton.Click += (_, _) =>
        {
            (TestSimSettings.AttackerBuildId, TestSimSettings.DefenderBuildId) =
                (TestSimSettings.DefenderBuildId, TestSimSettings.AttackerBuildId);
            TestSimLauncher.Rematch(_context);
        };

        logsButton.TouchDown += (_, _) =>
        {
            _combatLogWindow ??= new Window { Content = _combatLog };

            if (_combatLogWindow?.IsPlaced == true)
            {
                _combatLogWindow.Close();
                return;
            }

            _combatLogWindow!.Show(gui.Desktop);
            _combatLogWindow.Arrange(new Rectangle(0, 0, Core.ReferenceResolution.X, Core.ReferenceResolution.Y));
            var windowWidth = _combatLogWindow.ActualBounds.Width;
            var centerX = (Core.ReferenceResolution.X - windowWidth) / 2;
            _combatLogWindow.Left = centerX;
            _combatLogWindow.Top = 370;
        };

        _showSummaryButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Show Summary",
                TextColor = Color.Goldenrod
            }
        };
        _showSummaryButton.Click += (_, _) =>
        {
            ShowSummaryWindow();
            _showSummaryButton.Visible = false;
        };

        VerticalStackPanel centerColumn = new()
        {
            Margin = new Thickness(10, 0, 10, 0),
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                _controlPanel,
                _tickLabel,
                logsButton,
                rematchButton,
                rerollButton,
                swapButton,
                _showSummaryButton
            }
        };
        Grid.SetRow(centerColumn, 1);
        Grid.SetColumn(centerColumn, 1);

        var panel = new Panel()
        {
            Height = 1300,
            Widgets = { _enemyPawnBodyView }
        };
        Grid.SetRow(panel, 2);
        Grid.SetColumn(panel, 2);
        Grid grid = new()
        {
            Margin = new Thickness(30, 0, 30, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            RowSpacing = 0,
            DefaultRowProportion = Proportion.Auto,
            DefaultColumnProportion = Proportion.Auto,
        };
        var playerBodyPanel = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Widgets = { _pawnBodyView }
        };
        Grid.SetRow(playerBodyPanel, 2);
        Grid.SetColumn(playerBodyPanel, 0);

        var centerBodyPanel = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(centerBodyPanel, 2);
        Grid.SetColumn(centerBodyPanel, 1);

        grid.Widgets.Add(_playerPartyPanel);
        grid.Widgets.Add(centerColumn);
        grid.Widgets.Add(_opponentPartyPanel);
        grid.Widgets.Add(playerBodyPanel);
        grid.Widgets.Add(centerBodyPanel);
        grid.Widgets.Add(panel);

        Widgets.Add(_gameHud);
        Widgets.Add(grid);
    }

    // Render events to the pawn body widget 
    private void PrintDamage(CombatEvent combatEvent)
    {
        var color = combatEvent.Type switch
        {
            CombatEventType.Damage => new Color(186, 22, 0),
            CombatEventType.Block => new Color(0, 150, 237),
            CombatEventType.Dodge => new Color(0, 150, 237),
            CombatEventType.Miss => Color.Orange,
            CombatEventType.Heal => Color.GreenYellow,
            CombatEventType.Buff => Color.GreenYellow,
            CombatEventType.Debuff => new Color(237, 51, 0),
            CombatEventType.StatusEffect => Color.Purple,
            CombatEventType.Death => Color.AntiqueWhite,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Route damage text to the appropriate body widget
        var partyPanel = combatEvent.Target.PawnType == PawnType.Player
            ? _playerPartyPanel
            : _opponentPartyPanel;

        var combatPanel = partyPanel.GetPanelForPawn(combatEvent.Target);
        if (combatPanel?.BodyWidget != null)
        {
            // Try to extract a numeric damage value from the text for font scaling
            var damageAmount = 0f;
            if (combatEvent.Type == CombatEventType.Damage || combatEvent.Type == CombatEventType.Heal)
            {
                // Extract all digits from the text and try to parse as damage
                var digits = new string(combatEvent.Text.Where(char.IsDigit).ToArray());
                if (float.TryParse(digits, out var parsed))
                {
                    damageAmount = parsed;
                }
            }

            var font = combatEvent.Type switch
            {
                CombatEventType.Damage => BaseContent.Fonts.Default.VerySmall,
                CombatEventType.Heal => BaseContent.Fonts.Default.VerySmall,
                CombatEventType.Block => BaseContent.Fonts.Default.Smallest,
                CombatEventType.Dodge => BaseContent.Fonts.Default.Smallest,
                CombatEventType.Miss => BaseContent.Fonts.Default.Smallest,
                CombatEventType.Buff => BaseContent.Fonts.Default.Smallest,
                CombatEventType.Debuff => BaseContent.Fonts.Default.Smallest,
                CombatEventType.Death => BaseContent.Fonts.Default.Small,
                CombatEventType.StatusEffect => BaseContent.Fonts.Default.Smallest,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (combatEvent.IsCritical)
            {
                font = BaseContent.Fonts.Default.Normal;
                color = Color.Red;
            }
            combatPanel.BodyWidget.AddDamageText(combatEvent.BodyPart, combatEvent.Text, font, color, 3f);
        }
    }

    private void CombatStateChangedAction(EncounterState state)
    {
        switch (state)
        {
            case EncounterState.InProgress:
                break;
            case EncounterState.Finished:
                ShowCombatSummary();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void ShowCombatSummary()
    {
        Action onContinue = DebugSettings.TestSimMode
            ? () => TestSimLauncher.ReturnToSelector(_context)
            : () => Encounter.Zone.CombatResults();
        _summaryWindow = new CombatSummaryWindow(Encounter, onContinue);
        _summaryWindow.OnReviewRequested += OnReviewRequested;
        _summaryWindow.Show(_gui.Desktop);
        CenterSummaryWindow();
    }

    private void OnReviewRequested()
    {
        if (_showSummaryButton != null)
        {
            _showSummaryButton.Visible = true;
        }
    }

    private void ShowSummaryWindow()
    {
        if (_summaryWindow != null)
        {
            _summaryWindow.Visible = true;
            CenterSummaryWindow();
        }
    }

    private void CenterSummaryWindow()
    {
        if (_summaryWindow == null) return;

        _summaryWindow.Arrange(new Rectangle(0, 0, Core.ReferenceResolution.X, Core.ReferenceResolution.Y));
        var windowWidth = _summaryWindow.ActualBounds.Width;
        var centerX = (Core.ReferenceResolution.X - windowWidth) / 2;
        _summaryWindow.Left = centerX;
        _summaryWindow.Top = 250;
    }

    public void Update(float deltaTime)
    {
        _tickLabel.Text = $"{_context.CurrentZone?.ActiveEncounter?.Ticks}";
        _gameHud.Update();
        _playerPartyPanel.Update(deltaTime);
        _opponentPartyPanel.Update(deltaTime);
        _pawnBodyView.Update();
        _enemyPawnBodyView.Update();
    }

    private void AddCombatLogEntry(string? detailedMessage)
    {
        var panel = (VerticalStackPanel)_combatLog.Content;
        if (panel.Widgets.Count > 300)
        {
            panel.Widgets.RemoveAt(panel.Widgets.Count - 1);
        }

        Label label = new(BaseContent.Styles.Label.Small)
        {
            Width = 1600,
            Text = detailedMessage,
            Wrap = true,
            Margin = new Thickness(0, 10, 0, 0)
        };

        panel.Widgets.Insert(0, label);
    }

    public void Dispose()
    {
        Encounter.StateChangedAction -= CombatStateChangedAction;
        Encounter.CombatHandler!.CombatLogMessageAdded -= AddCombatLogEntry;
        Encounter.CombatHandler!.EventOccured -= PrintDamage;
        if (_summaryWindow != null)
        {
            _summaryWindow.OnReviewRequested -= OnReviewRequested;
        }
    }
}