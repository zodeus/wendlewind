using Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
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
    private readonly CombatFloaterRouter _floaterRouter;
    private readonly CombatFighterStatsColumn _playerStats;
    private readonly CombatFighterStatsColumn _opponentStats;
    private readonly CombatConsumableLoadout _playerLoadout;
    private readonly CombatConsumableLoadout _opponentLoadout;
    private readonly Widget _playerCenter;
    private readonly Widget _opponentCenter;

    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatScreen(ZoneGui gui, GameContext context)
    {
        _gui = gui;
        _context = context;
        Encounter.StateChangedAction += CombatStateChangedAction;
        Encounter.CombatHandler!.CombatEventRecorded += OnCombatEvent;
        Margin = new Thickness(0, 5, 0, 0);

        _playerPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.PlayerPawns, HorizontalAlignment.Right);
        _opponentPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.EnemyPawns, HorizontalAlignment.Left);

        _controlPanel = new CombatControlPanel(Encounter)
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var player = Encounter.PlayerPawns.First();
        var opponent = Encounter.EnemyPawns.First();
        _pawnBodyView = new PawnBodyPanel(gui, player.Body, fillAvailableHeight: true)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _enemyPawnBodyView = new PawnBodyPanel(gui, opponent.Body, fillAvailableHeight: true)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _combatLog = new ScrollViewer
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 1450,
            Height = 620,
            Content = new VerticalStackPanel { Padding = new Thickness(0), Spacing = 12 }
        };

        _tickLabel = new Label(BaseContent.Styles.Label.Small)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text = "0",
            TextColor = Color.DarkGoldenrod
        };

        var logsButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Logs"
            }
        };
        var rematchButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Visible = DebugSettings.TestSimMode,
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Rematch" }
        };
        rematchButton.Click += (_, _) => TestSimLauncher.Rematch(_context);

        var rerollButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Visible = DebugSettings.TestSimMode,
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Reroll" }
        };
        rerollButton.Click += (_, _) => TestSimLauncher.Reroll(_context);

        var swapButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            VerticalAlignment = VerticalAlignment.Center,
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
            VerticalAlignment = VerticalAlignment.Center,
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

        var toolbar = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
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
        _gameHud = new GameHud(gui, context, toolbar)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        _playerStats = new CombatFighterStatsColumn(player);
        _opponentStats = new CombatFighterStatsColumn(opponent);
        _playerLoadout = new CombatConsumableLoadout(gui, player)
        {
            Margin = new Thickness(0, 8, 0, 0)
        };
        _opponentLoadout = new CombatConsumableLoadout(gui, opponent, mirror: true)
        {
            Margin = new Thickness(0, 8, 0, 0)
        };

        _playerCenter = new VerticalStackPanel
        {
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Top,
            Widgets = { _playerStats, _playerLoadout }
        };
        _opponentCenter = new VerticalStackPanel
        {
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Top,
            Widgets = { _opponentStats, _opponentLoadout }
        };

        Grid.SetRow(_playerPartyPanel, 0);
        Grid.SetColumn(_playerPartyPanel, 0);
        Grid.SetColumnSpan(_playerPartyPanel, 2);

        Grid.SetRow(_opponentPartyPanel, 0);
        Grid.SetColumn(_opponentPartyPanel, 2);
        Grid.SetColumnSpan(_opponentPartyPanel, 2);

        Grid.SetRow(_pawnBodyView, 1);
        Grid.SetColumn(_pawnBodyView, 0);

        Grid.SetRow(_playerCenter, 1);
        Grid.SetColumn(_playerCenter, 1);

        Grid.SetRow(_opponentCenter, 1);
        Grid.SetColumn(_opponentCenter, 2);

        Grid.SetRow(_enemyPawnBodyView, 1);
        Grid.SetColumn(_enemyPawnBodyView, 3);

        Grid grid = new()
        {
            Margin = new Thickness(30, 0, 30, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            RowSpacing = 8,
            ColumnSpacing = 17,
            DefaultRowProportion = Proportion.Auto,
            DefaultColumnProportion = Proportion.Auto,
        };
        grid.Widgets.Add(_playerPartyPanel);
        grid.Widgets.Add(_opponentPartyPanel);
        grid.Widgets.Add(_pawnBodyView);
        grid.Widgets.Add(_playerCenter);
        grid.Widgets.Add(_opponentCenter);
        grid.Widgets.Add(_enemyPawnBodyView);

        Widgets.Add(_gameHud);
        Widgets.Add(grid);

        _floaterRouter = new CombatFloaterRouter(
            _playerPartyPanel,
            _opponentPartyPanel,
            _pawnBodyView,
            _enemyPawnBodyView,
            Encounter.PlayerPawns,
            Encounter.EnemyPawns);
        _floaterRouter.MedicalUsed += OnMedicalUsed;
    }

    private void OnCombatEvent(CombatLogEvent combatEvent)
    {
        var logLine = CombatLogFormatter.Format(combatEvent);
        if (!string.IsNullOrEmpty(logLine))
        {
            AddCombatLogEntry(logLine);
        }

        _floaterRouter.Handle(combatEvent);
        ApplyEquipmentFeedback(combatEvent);
    }

    private void ApplyEquipmentFeedback(CombatLogEvent combatEvent)
    {
        if (combatEvent.Kind == CombatEventKind.Damage)
        {
            FlashEquipment(combatEvent.ItemMoniker, EquipmentFlashKind.Strike);
            if (combatEvent.Blocked > 0)
            {
                FlashEquipment(combatEvent.BlockingItemMoniker, EquipmentFlashKind.Block);
            }
        }
        else if (combatEvent.Kind == CombatEventKind.PotionUsed)
        {
            FlashEquipment(combatEvent.ItemMoniker, EquipmentFlashKind.Potion);
        }

        foreach (var sub in combatEvent.SubEffects)
        {
            if (sub.Kind == CombatEventKind.StatusReflected)
            {
                FlashEquipment(sub.ItemMoniker, EquipmentFlashKind.Proc);
            }
            else if (sub.Kind == CombatEventKind.EquipmentDestroyed)
            {
                FlashEquipment(sub.ItemMoniker, EquipmentFlashKind.Destroyed);
            }
        }
    }

    private void FlashEquipment(string? moniker, EquipmentFlashKind kind)
    {
        if (string.IsNullOrEmpty(moniker))
        {
            return;
        }

        foreach (var pawn in Encounter.PlayerPawns)
        {
            _playerPartyPanel.GetPanelForPawn(pawn)?.EquipmentPanel?.FlashSlot(moniker, kind);
        }

        foreach (var pawn in Encounter.EnemyPawns)
        {
            _opponentPartyPanel.GetPanelForPawn(pawn)?.EquipmentPanel?.FlashSlot(moniker, kind);
        }
    }

    private void OnMedicalUsed(CombatLogEvent combatEvent)
    {
        ResolveLoadout(combatEvent.SubjectPawnId).NotifyMedicalUsed(combatEvent.ItemMoniker);
    }

    private CombatConsumableLoadout ResolveLoadout(int pawnId)
    {
        return Encounter.PlayerPawns.Any(p => p.Id == pawnId)
            ? _playerLoadout
            : _opponentLoadout;
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
        _pawnBodyView.Update(deltaTime);
        _enemyPawnBodyView.Update(deltaTime);
        _floaterRouter.Update(deltaTime);
        _playerStats.Update();
        _opponentStats.Update();
        _playerLoadout.Update();
        _opponentLoadout.Update();
        SyncBodyPanelHeights();
    }

    private void SyncBodyPanelHeights()
    {
        var height = Math.Max(_playerCenter.ActualBounds.Height, _opponentCenter.ActualBounds.Height);
        if (height <= 0)
        {
            return;
        }

        if (_pawnBodyView.Height != height)
        {
            _pawnBodyView.Height = height;
        }

        if (_enemyPawnBodyView.Height != height)
        {
            _enemyPawnBodyView.Height = height;
        }
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
        Encounter.CombatHandler!.CombatEventRecorded -= OnCombatEvent;
        _floaterRouter.MedicalUsed -= OnMedicalUsed;
        if (_summaryWindow != null)
        {
            _summaryWindow.OnReviewRequested -= OnReviewRequested;
        }
    }
}