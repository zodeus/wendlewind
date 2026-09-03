using Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;
using TestSimLauncher = Wendlemire.Scenes.MainGameScene.TestSimLauncher;
using TestSimSettings = Wendlemire.Scenes.MainGameScene.TestSimSettings;

namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

public class CombatScreen : VerticalStackPanel, IDisposable
{
    private readonly BaseGui _gui;
    private readonly GameContext _context;
    private readonly Action? _onFinished;
    private readonly Action? _onCombatEnded;
    private readonly ScrollViewer _combatLog;
    private readonly GameHud _gameHud;
    private readonly CombatPartyPanel _playerPartyPanel;
    private readonly CombatPartyPanel _opponentPartyPanel;
    private readonly CombatControlPanel _controlPanel;
    private readonly PawnBodyPanel _pawnBodyView;
    private readonly PawnBodyPanel _enemyPawnBodyView;
    private readonly Label _tickLabel;
    private bool _wastingActive;
    private Window? _combatLogWindow;
    private CombatSummaryWindow? _summaryWindow;
    private CursorButton? _showSummaryButton;
    private bool _summaryPending;
    private readonly CombatFloaterRouter _floaterRouter;
    private readonly CombatPotionThrowFx _potionThrowFx;
    private readonly CombatMedicalTravelFx _medicalTravelFx;
    private readonly CombatIncenseSmokeFx _incenseSmokeFx;
    private readonly CombatFighterStatsColumn _playerStats;
    private readonly CombatFighterStatsColumn _opponentStats;
    private readonly CombatConsumableLoadout _playerLoadout;
    private readonly CombatConsumableLoadout _opponentLoadout;
    private readonly Widget _playerCenter;
    private readonly Widget _opponentCenter;
    private readonly List<CombatLogEvent> _pendingPotionThrows = [];
    private readonly List<CombatLogEvent> _pendingMedicalTravels = [];

    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatScreen(BaseGui gui, GameContext context, Action? onFinished = null, Action? onCombatEnded = null)
    {
        _gui = gui;
        _context = context;
        _onFinished = onFinished;
        _onCombatEnded = onCombatEnded;
        Encounter.StateChangedAction += CombatStateChangedAction;
        Encounter.CombatHandler!.CombatEventRecorded += OnCombatEvent;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Margin = new Thickness(0, 5, 0, 0);

        _playerPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.PlayerPawns, HorizontalAlignment.Right);
        _opponentPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.EnemyPawns, HorizontalAlignment.Left);

        _controlPanel = new CombatControlPanel(Encounter)
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var player = Encounter.PlayerPawns.First();
        var opponent = Encounter.EnemyPawns.First();
        _pawnBodyView = new PawnBodyPanel(gui, player.Body, fillAvailableHeight: true, hoverToInspect: true, pawn: player)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _enemyPawnBodyView = new PawnBodyPanel(gui, opponent.Body, fillAvailableHeight: true, hoverToInspect: true, pawn: opponent)
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
        _opponentLoadout = new CombatConsumableLoadout(gui, opponent)
        {
            Margin = new Thickness(0, 8, 0, 0)
        };

        _playerCenter = new VerticalStackPanel
        {
            Spacing = 0,
            ClipToBounds = false,
            VerticalAlignment = VerticalAlignment.Top,
            Widgets = { _playerStats, _playerLoadout }
        };
        _opponentCenter = new VerticalStackPanel
        {
            Spacing = 0,
            ClipToBounds = false,
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
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = false,
            RowSpacing = 8,
            ColumnSpacing = 17,
            DefaultColumnProportion = Proportion.Auto,
        };
        grid.RowsProportions.Add(Proportion.Auto);
        grid.RowsProportions.Add(new Proportion(ProportionType.Fill));
        grid.Widgets.Add(_playerPartyPanel);
        grid.Widgets.Add(_opponentPartyPanel);
        grid.Widgets.Add(_pawnBodyView);
        grid.Widgets.Add(_playerCenter);
        grid.Widgets.Add(_opponentCenter);
        grid.Widgets.Add(_enemyPawnBodyView);

        Widgets.Add(_gameHud);
        Widgets.Add(grid);
        SetProportionType(_gameHud, ProportionType.Auto);
        SetProportionType(grid, ProportionType.Fill);

        _floaterRouter = new CombatFloaterRouter(
            _playerPartyPanel,
            _opponentPartyPanel,
            _pawnBodyView,
            _enemyPawnBodyView,
            Encounter.PlayerPawns,
            Encounter.EnemyPawns);
        _floaterRouter.MedicalUsed += OnMedicalUsed;
        _floaterRouter.PotionUsed += OnPotionUsed;
        _floaterRouter.IncenseLit += OnIncenseLit;

        _potionThrowFx = new CombatPotionThrowFx();
        _potionThrowFx.Impacted += _floaterRouter.ShowPotionImpact;
        Grid.SetRow(_potionThrowFx, 0);
        Grid.SetRowSpan(_potionThrowFx, 2);
        Grid.SetColumn(_potionThrowFx, 0);
        Grid.SetColumnSpan(_potionThrowFx, 4);
        grid.Widgets.Add(_potionThrowFx);

        _medicalTravelFx = new CombatMedicalTravelFx();
        Grid.SetRow(_medicalTravelFx, 0);
        Grid.SetRowSpan(_medicalTravelFx, 2);
        Grid.SetColumn(_medicalTravelFx, 0);
        Grid.SetColumnSpan(_medicalTravelFx, 4);
        grid.Widgets.Add(_medicalTravelFx);

        _incenseSmokeFx = new CombatIncenseSmokeFx();
        Grid.SetRow(_incenseSmokeFx, 0);
        Grid.SetRowSpan(_incenseSmokeFx, 2);
        Grid.SetColumn(_incenseSmokeFx, 0);
        Grid.SetColumnSpan(_incenseSmokeFx, 4);
        grid.Widgets.Add(_incenseSmokeFx);
    }

    private void OnCombatEvent(CombatLogEvent combatEvent)
    {
        var logLine = CombatLogFormatter.Format(combatEvent);
        if (!string.IsNullOrEmpty(logLine))
        {
            AddCombatLogEntry(logLine);
        }

        if (combatEvent.Kind == CombatEventKind.System && combatEvent.Message == CombatCloser.StartedMessage)
        {
            ActivateWastingUi();
        }

        _floaterRouter.Handle(combatEvent);
        ApplyEquipmentFeedback(combatEvent);
    }

    private void ActivateWastingUi()
    {
        if (_wastingActive)
        {
            return;
        }

        _wastingActive = true;
        _playerStats.SetWasting(true);
        _opponentStats.SetWasting(true);
        _gui.PushScreenMessage(new ScreenMessageData
        {
            Text = CombatCloser.StartedMessage.ToUpperInvariant(),
            Duration = 10,
            Color = Color.OrangeRed
        });
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

    private void OnIncenseLit(CombatLogEvent combatEvent)
    {
        var pawn = FindPawn(combatEvent.SubjectPawnId);
        if (pawn == null)
        {
            return;
        }

        var tint = CombatIncenseSmokeFx.TintFor(combatEvent.ItemMoniker);
        var portrait = ResolveParty(pawn).GetPanelForPawn(pawn)?.BodyWidget;
        if (portrait == null || portrait.Bounds.Width <= 0 || portrait.Bounds.Height <= 0)
        {
            return;
        }

        _incenseSmokeFx.TryStart(
            portrait,
            PortraitIncenseOrigin(portrait),
            tint);
    }

    private void OnPotionUsed(CombatLogEvent combatEvent)
    {
        if (TryLaunchPotionThrow(combatEvent))
        {
            return;
        }

        _pendingPotionThrows.Add(combatEvent);
    }

    private void FlushPendingPotionThrows()
    {
        for (var i = 0; i < _pendingPotionThrows.Count;)
        {
            var combatEvent = _pendingPotionThrows[i];
            if (TryLaunchPotionThrow(combatEvent))
            {
                _pendingPotionThrows.RemoveAt(i);
                continue;
            }

            if (HasArrangedPotionTarget(combatEvent))
            {
                _floaterRouter.ShowPotionImpact(combatEvent);
                _pendingPotionThrows.RemoveAt(i);
                continue;
            }

            i++;
        }
    }

    private bool HasArrangedPotionTarget(CombatLogEvent combatEvent)
    {
        var target = FindPawn(combatEvent.TargetPawnId ?? combatEvent.SubjectPawnId);
        var portrait = target != null
            ? ResolveParty(target).GetPanelForPawn(target)?.BodyWidget
            : null;
        return portrait is { Bounds.Width: > 0, Bounds.Height: > 0 };
    }

    private bool TryLaunchPotionThrow(CombatLogEvent combatEvent)
    {
        if (string.IsNullOrEmpty(combatEvent.ItemMoniker))
        {
            return false;
        }

        var user = FindPawn(combatEvent.SubjectPawnId);
        var target = FindPawn(combatEvent.TargetPawnId ?? combatEvent.SubjectPawnId);
        if (user == null || target == null)
        {
            return false;
        }

        var userPanel = ResolveParty(user).GetPanelForPawn(user);
        var targetPanel = ResolveParty(target).GetPanelForPawn(target);
        var portrait = targetPanel?.BodyWidget;
        if (userPanel == null || portrait == null || portrait.Bounds.Width <= 0 || portrait.Bounds.Height <= 0)
        {
            return false;
        }

        if (!TryResolvePotionThrowOrigin(userPanel, combatEvent.ItemMoniker, out var source, out var startLocal))
        {
            return false;
        }

        var def = DefRepository<ItemDef>.GetByMoniker(combatEvent.ItemMoniker, raiseError: false);
        if (def == null)
        {
            return false;
        }

        var endLocal = new Vector2(portrait.Bounds.Width * 0.5f, portrait.Bounds.Height * 0.5f);
        return _potionThrowFx.TryStart(
            combatEvent,
            source,
            startLocal,
            portrait,
            endLocal,
            def.GetIcon(),
            thrown: user.Id != target.Id);
    }

    private static bool TryResolvePotionThrowOrigin(
        PawnCombatPanel userPanel,
        string itemMoniker,
        out Widget source,
        out Vector2 startLocal)
    {
        if (userPanel.EquipmentPanel != null
            && userPanel.EquipmentPanel.TryGetSlotCenter(itemMoniker, out startLocal))
        {
            source = userPanel.EquipmentPanel;
            return true;
        }

        var userPortrait = userPanel.BodyWidget;
        if (userPortrait is { Bounds.Width: > 0, Bounds.Height: > 0 })
        {
            source = userPortrait;
            startLocal = new Vector2(userPortrait.Bounds.Width * 0.5f, userPortrait.Bounds.Height * 0.5f);
            return true;
        }

        source = null!;
        startLocal = default;
        return false;
    }

    private CombatPartyPanel ResolveParty(Pawn pawn)
    {
        return pawn.PawnType == PawnType.Player ? _playerPartyPanel : _opponentPartyPanel;
    }

    private Pawn? FindPawn(int pawnId)
    {
        foreach (var pawn in Encounter.PlayerPawns)
        {
            if (pawn.Id == pawnId)
            {
                return pawn;
            }
        }

        foreach (var pawn in Encounter.EnemyPawns)
        {
            if (pawn.Id == pawnId)
            {
                return pawn;
            }
        }

        return null;
    }

    private void OnMedicalUsed(CombatLogEvent combatEvent)
    {
        ResolveLoadout(combatEvent.SubjectPawnId).NotifyMedicalUsed(combatEvent.ItemMoniker);
        if (!TryLaunchMedicalTravel(combatEvent))
        {
            _pendingMedicalTravels.Add(combatEvent);
        }
    }

    private void FlushPendingMedicalTravels()
    {
        for (var i = 0; i < _pendingMedicalTravels.Count;)
        {
            var combatEvent = _pendingMedicalTravels[i];
            if (FindPawn(combatEvent.SubjectPawnId) == null || TryLaunchMedicalTravel(combatEvent))
            {
                _pendingMedicalTravels.RemoveAt(i);
                continue;
            }

            i++;
        }
    }

    private bool TryLaunchMedicalTravel(CombatLogEvent combatEvent)
    {
        var pawn = FindPawn(combatEvent.SubjectPawnId);
        if (pawn == null)
        {
            return false;
        }

        var loadout = ResolveLoadout(combatEvent.SubjectPawnId);
        if (!loadout.TryGetMedicalSlot(combatEvent.ItemMoniker, out var source) || source.Bounds.Width <= 0)
        {
            return false;
        }

        var body = pawn.PawnType == PawnType.Player ? _pawnBodyView : _enemyPawnBodyView;
        var target = body.FindPartWidget(combatEvent.BodyPartKey);
        if (target == null || target.Bounds.Width <= 0)
        {
            var portrait = ResolveParty(pawn).GetPanelForPawn(pawn)?.BodyWidget;
            if (portrait == null || portrait.Bounds.Width <= 0)
            {
                return false;
            }

            target = portrait;
        }

        var tint = combatEvent.ItemMoniker == Defs.Items.Cauterize.Moniker
            ? new Color(255, 140, 50)
            : new Color(90, 210, 140);
        return _medicalTravelFx.TryStart(
            source,
            new Vector2(source.Bounds.Width * 0.5f, source.Bounds.Height * 0.5f),
            target,
            new Vector2(target.Bounds.Width * 0.5f, target.Bounds.Height * 0.5f),
            tint);
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
                _onCombatEnded?.Invoke();
                // Scene.Update runs before FixedUpdate, so this opens on the next frame
                // instead of stacking window layout with the death tick.
                _summaryPending = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void ShowCombatSummary()
    {
        Action onContinue = _onFinished
            ?? (DebugSettings.TestSimMode
                ? () => TestSimLauncher.ReturnToSelector(_context)
                : () => Encounter.Zone.CombatResults());
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
        var bounds = _summaryWindow.ActualBounds;
        _summaryWindow.Left = (Core.ReferenceResolution.X - bounds.Width) / 2;
        _summaryWindow.Top = Math.Max(80, (Core.ReferenceResolution.Y - bounds.Height) / 2);
    }

    public void Update(float deltaTime)
    {
        if (_summaryPending)
        {
            _summaryPending = false;
            ShowCombatSummary();
        }

        var ticks = _context.CurrentZone?.ActiveEncounter?.Ticks ?? 0;
        if (!_wastingActive && CombatCloser.IsActive(ticks))
        {
            ActivateWastingUi();
        }

        if (_wastingActive)
        {
            var remaining = Math.Max(0, CombatCloser.HardResolveTicks - ticks);
            _tickLabel.Text = $"Wasting {remaining / (float)GameContext.TicksPerSecond:0}s remaining";
            _tickLabel.TextColor = new Color(230, 50, 30);
        }
        else
        {
            _tickLabel.Text = $"{ticks}";
            _tickLabel.TextColor = Color.DarkGoldenrod;
        }

        _gameHud.Update();
        _playerPartyPanel.Update(deltaTime);
        _opponentPartyPanel.Update(deltaTime);
        _pawnBodyView.Update(deltaTime);
        _enemyPawnBodyView.Update(deltaTime);
        _floaterRouter.Update(deltaTime);
        FlushPendingPotionThrows();
        FlushPendingMedicalTravels();
        _potionThrowFx.Update(deltaTime);
        _medicalTravelFx.Update(deltaTime);
        _playerStats.Update();
        _opponentStats.Update();
        _playerLoadout.Update();
        _opponentLoadout.Update();
        _incenseSmokeFx.Sync(CollectIncenseBurns());
        _incenseSmokeFx.Update(deltaTime);
    }

    private List<CombatIncenseSmokeFx.BurnSource> CollectIncenseBurns()
    {
        var sources = new List<CombatIncenseSmokeFx.BurnSource>();
        AddIncenseBurns(_playerLoadout, _playerPartyPanel, sources);
        AddIncenseBurns(_opponentLoadout, _opponentPartyPanel, sources);
        return sources;
    }

    private static Vector2 PortraitIncenseOrigin(Widget portrait)
    {
        return new Vector2(portrait.Bounds.Width * 0.5f, portrait.Bounds.Height * 0.62f);
    }

    private static void AddIncenseBurns(
        CombatConsumableLoadout loadout,
        CombatPartyPanel party,
        List<CombatIncenseSmokeFx.BurnSource> sources)
    {
        var pawn = loadout.Pawn;
        for (var i = 0; i < pawn.ActiveIncense.Count; i++)
        {
            var incense = pawn.ActiveIncense[i];
            if (!loadout.IsBurning(incense))
            {
                continue;
            }

            var portrait = party.GetPanelForPawn(pawn)?.BodyWidget;
            if (portrait == null || portrait.Bounds.Width <= 0)
            {
                continue;
            }

            sources.Add(new CombatIncenseSmokeFx.BurnSource(
                $"portrait-{pawn.Id}-{i}",
                portrait,
                PortraitIncenseOrigin(portrait),
                CombatIncenseSmokeFx.TintFor(incense)));
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
        _floaterRouter.PotionUsed -= OnPotionUsed;
        _floaterRouter.IncenseLit -= OnIncenseLit;
        _potionThrowFx.Impacted -= _floaterRouter.ShowPotionImpact;
        if (_summaryWindow != null)
        {
            _summaryWindow.OnReviewRequested -= OnReviewRequested;
        }
    }
}