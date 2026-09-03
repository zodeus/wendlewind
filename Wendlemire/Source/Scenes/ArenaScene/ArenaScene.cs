using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Scenes.ArenaScene.Gui;
using Wendlemire.Scenes.Components;
using Wendlemire.Scenes.MainGameScene;

namespace Wendlemire.Scenes.ArenaScene;

public sealed class ArenaScene : Scene
{
    public const string SavePath = "arena-save.xml";
    public static bool StartFresh { get; set; }

    private readonly IServiceScope _runScope = SimServices.BuildRoot().CreateScope();
    private readonly WorldTextHandler _worldTextHandler = new();
    private GameContext _context = null!;
    private ArenaGui? _gui;
    private ArenaMatchClient? _client;
    private PlayerProfile _profile = null!;
    private string _runId = "";
    private DateTimeOffset _runStartedAt = DateTimeOffset.UtcNow;
    private BuildSnapshot? _lastPrepSnapshot;
    private CombatResult? _pendingResult;
    private BuildSnapshot? _recordedLoadout;
    private bool _matchResultRecorded;
    private bool _runFinishedOnServer;
    private Task? _matchTask;
    private readonly object _saveLock = new();
    private Task? _saveTask;
    private bool _saveBusy;
    private bool _saveDirty;
    private ArenaProgressRecord? _pendingProgress;
    private AchievementState? _pendingAchievements;
    private KeyboardState _previousKeyboardState;
    private float _autosaveTimer;
    private Desktop? _loadingDesktop;
    private BusyOverlay? _loadingOverlay;
    private Task<ArenaLoadPayload>? _loadTask;
    private Task? _leaveTask;
    private bool _leaving;

    public string? MatchError { get; private set; }
    public ArenaRankDisplay CurrentRank { get; private set; } = ArenaRank.FromRating(ArenaRank.StartingRating, 0);
    public ArenaRunRecord? LastFinishedRun { get; private set; }
    private string _equippedNamePlate = ArenaMarks.DefaultNamePlate;
    public CombatResult? LastCombatResult => _pendingResult;

    protected override void OnStart()
    {
        MatchError = null;
        _pendingResult = null;
        _recordedLoadout = null;
        _matchResultRecorded = false;
        _runFinishedOnServer = false;
        _matchTask = null;
        _lastPrepSnapshot = null;
        _profile = PlayerProfile.LoadOrCreate();
        _context = _runScope.ServiceProvider.GetRequiredService<GameContext>();
        Core.Context = _context;
        _client = new ArenaMatchClient();

        TryDeleteLocalSave();

        var startFresh = StartFresh;
        StartFresh = false;
        _loadingOverlay = new BusyOverlay();
        _loadingDesktop = new Desktop
        {
            Root = _loadingOverlay,
            HasExternalTextInput = true
        };
        Core.ConfigureDesktopScaling(_loadingDesktop);
        _loadingOverlay.Show("Loading arena...");
        _loadTask = Task.Run(() => FetchLoadAsync(startFresh));
    }

    public override void End()
    {
        WaitForBackground(_loadTask);
        WaitForBackground(_leaveTask);
        SaveOnExit();
        _gui?.Dispose();
        _gui = null;
        _loadingDesktop = null;
        _loadingOverlay = null;
        _client?.Dispose();
        _client = null;
        _worldTextHandler.Clear();
    }

    public override void Update(float deltaTime)
    {
        _loadingOverlay?.Update(deltaTime);
        PollLoad();
        PollLeave();
        if (_gui == null)
        {
            return;
        }

        HandleInput();
        PollMatch();
        AutosavePrep(deltaTime);
        _gui.Update(deltaTime);
    }

    public override void FixedUpdate()
    {
        if (_context.ArenaRun?.Phase != ArenaPhase.Combat)
        {
            return;
        }

        for (var i = 0; i < DebugSettings.CombatSpeed; i++)
        {
            _context.Tick();
            _worldTextHandler.Tick();
        }
    }

    public override void Draw(float deltaTime)
    {
        if (_gui != null)
        {
            _gui.Draw(Core.Graphics.Batcher, deltaTime);
            _worldTextHandler.Render(Core.Graphics.Batcher, deltaTime);
            return;
        }

        Core.GraphicsDevice.Clear(new Color(7, 5, 4));
        _loadingDesktop?.Render();
    }

    public void FinishShopping()
    {
        _context.ArenaRun?.SetPhase(ArenaPhase.Prep);
        SaveRun();
    }

    public void ReturnToShop()
    {
        var run = _context.ArenaRun;
        if (run?.CurrentMerchant == null)
        {
            return;
        }

        run.SetPhase(run.FightsPlayed == 0 ? ArenaPhase.GeneralStore : ArenaPhase.Shop);
        SaveRun();
    }

    public void BeginFight()
    {
        if (_context.ArenaRun == null || _client == null)
        {
            return;
        }

        MatchError = null;
        _pendingResult = null;
        _recordedLoadout = null;
        _matchResultRecorded = false;
        var round = _context.ArenaRun.FightsPlayed + 1;
        _lastPrepSnapshot = BuildSnapshotFactory.ToSnapshot(
            _context.PlayerPawn,
            _context.ArenaRun.PlayerId,
            buildId: $"arena-{round}",
            seed: _context.RunSeed,
            round: round,
            rating: CurrentRank.Rating);
        _context.ArenaRun.SetPhase(ArenaPhase.Matching);
        var snapshot = _lastPrepSnapshot;
        SaveRun();
        _matchTask = Task.Run(() => RequestMatch(snapshot));
    }

    public void ReturnToPrep()
    {
        MatchError = null;
        _context.ArenaRun?.SetPhase(ArenaPhase.Prep);
        SaveRun();
    }

    public void SaveProgress()
    {
        SaveRun();
    }

    public void RecordVisualCombatResult()
    {
        RecordMatchIfNeeded();
    }

    public void ReplayVisualDuel()
    {
        if (_pendingResult == null)
        {
            return;
        }

        StartVisualDuel(_pendingResult);
    }

    public void OnVisualCombatFinished()
    {
        var run = _context.ArenaRun;
        if (run == null || _pendingResult == null || _lastPrepSnapshot == null)
        {
            run?.SetPhase(ArenaPhase.Prep);
            return;
        }

        RecordMatchIfNeeded();
        RestorePawnAfterCombat();

        if (run.IsRunOver)
        {
            FinishOnServer(run.IsVictory);
            run.SetPhase(ArenaPhase.RunEnd);
            return;
        }

        run.SetPhase(ArenaPhase.MerchantSelect);
    }

    public void ContinueFromResults()
    {
        var run = _context.ArenaRun;
        if (run == null)
        {
            return;
        }

        if (run.IsRunOver)
        {
            SaveRun();
            FinishOnServer(run.IsVictory);
            run.SetPhase(ArenaPhase.RunEnd);
            return;
        }

        run.AssignNextMerchant();
        run.SetPhase(ArenaPhase.MerchantSelect);
        SaveRun();
    }

    public void SelectMerchant(MerchantDef merchant)
    {
        if (_context.ArenaRun == null)
        {
            return;
        }

        _context.ArenaRun.CurrentMerchant = merchant;
        _context.ArenaRun.SetPhase(ArenaPhase.Shop);
        SaveRun();
    }

    public void ReturnToMenu()
    {
        if (_leaving || _gui == null)
        {
            return;
        }

        _leaving = true;
        _gui.ShowBusy("Saving...");
        var finish = _context.ArenaRun?.IsRunOver == true;
        var victory = _context.ArenaRun?.IsVictory ?? false;
        var achievements = finish ? ExportAchievements() : null;
        if (!finish)
        {
            SaveRun();
        }

        _leaveTask = Task.Run(() => LeaveToMenuAsync(finish, victory, achievements));
    }

    private async Task<ArenaLoadPayload> FetchLoadAsync(bool startFresh)
    {
        var payload = new ArenaLoadPayload { IsStartFresh = startFresh };
        payload.EnsuredProfile = await TryGetAsync(() =>
            _client!.EnsureProfile(_profile.PlayerId, _profile.DisplayName, _profile.Username));
        payload.Profile = await TryGetAsync(() => _client!.GetProfile(_profile.PlayerId));
        payload.Current = await TryGetAsync(() => _client!.GetCurrentArena(_profile.PlayerId));
        var displayName = payload.EnsuredProfile != null && !string.IsNullOrWhiteSpace(payload.EnsuredProfile.DisplayName)
            ? payload.EnsuredProfile.DisplayName
            : _profile.DisplayName;
        if (startFresh || payload.Current == null)
        {
            var reuseSeed = startFresh && payload.Current is { RunSeed: > 0 } ? payload.Current.RunSeed : 0;
            payload.Started = await TryGetAsync(() =>
                _client!.StartArena(_profile.PlayerId, displayName, reuseSeed));
        }

        payload.Achievements = await TryGetAsync(() => _client!.GetAchievements(_profile.PlayerId));
        return payload;
    }

    private void PollLoad()
    {
        if (_loadTask is not { IsCompleted: true } task)
        {
            return;
        }

        _loadTask = null;
        ArenaLoadPayload payload;
        if (task.IsCompletedSuccessfully)
        {
            payload = task.Result;
        }
        else
        {
            MatchError = task.Exception?.GetBaseException().Message;
            Log.Warning($"Arena server request failed: {MatchError}");
            payload = new ArenaLoadPayload { IsStartFresh = true };
        }

        ApplyLoad(payload);
    }

    private void ApplyLoad(ArenaLoadPayload payload)
    {
        if (payload.EnsuredProfile != null && !string.IsNullOrWhiteSpace(payload.EnsuredProfile.DisplayName))
        {
            _profile.DisplayName = payload.EnsuredProfile.DisplayName;
        }

        ApplyRankFromProfile(payload.Profile);

        if (!payload.IsStartFresh && payload.Current != null)
        {
            RestoreProgress(payload.Current);
        }
        else
        {
            var started = payload.Started;
            var runSeed = started?.RunSeed > 0 ? started.RunSeed : Random.Shared.Next();
            _context.InitializeArena(_profile.PlayerId, _profile.DisplayName, runSeed);
            _runId = started?.RunId ?? Guid.NewGuid().ToString("N");
            _runStartedAt = started?.StartedAt ?? DateTimeOffset.UtcNow;
            SaveRun();
        }

        ApplyEquippedNamePlate();
        ImportAchievements(payload.Achievements);
        _gui = new ArenaGui(_context, this, _worldTextHandler);
        _loadingDesktop = null;
        _loadingOverlay = null;
    }

    private void PollLeave()
    {
        if (_leaveTask is not { IsCompleted: true })
        {
            return;
        }

        _leaveTask = null;
        Core.ChangeScene<MainMenuScene>();
    }

    private async Task LeaveToMenuAsync(bool finish, bool victory, AchievementState? achievements)
    {
        await FlushPendingSaveAsync();
        if (!finish || _client == null)
        {
            return;
        }

        try
        {
            var run = await _client.FinishArena(_profile.PlayerId, victory);
            if (achievements != null)
            {
                await _client.SaveAchievements(_profile.PlayerId, achievements);
            }

            if (run != null)
            {
                LastFinishedRun = run;
                _runFinishedOnServer = true;
            }
        }
        catch (Exception ex)
        {
            MatchError = ex.Message;
            Log.Warning($"Arena server request failed: {ex.Message}");
        }
    }

    private async Task RequestMatch(BuildSnapshot snapshot)
    {
        try
        {
            await _client!.SubmitBuild(snapshot);
            var result = await _client.RequestMatch(snapshot);
            _pendingResult = result;
        }
        catch (Exception ex)
        {
            MatchError = ex.Message;
        }
    }

    private void PollMatch()
    {
        if (_context.ArenaRun?.Phase != ArenaPhase.Matching)
        {
            return;
        }

        if (MatchError != null && _matchTask is { IsCompleted: true })
        {
            _matchTask = null;
            _gui = RecreateGui();
            return;
        }

        if (_pendingResult?.Defender == null)
        {
            return;
        }

        StartVisualDuel(_pendingResult);
    }

    private void StartVisualDuel(CombatResult result)
    {
        EnsureZoneShell();
        _context.RestoreArenaPawn();
        var opponent = BuildSnapshotFactory.CreatePawn(_context, result.Defender!, PawnType.Enemy);
        BuildSnapshotFactory.Apply(_context.PlayerPawn, _lastPrepSnapshot!);
        _context.CurrentZone!.StartHumanDuel(
            _context.PlayerPawn,
            opponent,
            result.EncounterSeed != 0 ? result.EncounterSeed : _context.RunSeed);
        _context.ArenaRun!.SetPhase(ArenaPhase.Combat);
    }

    private void EnsureZoneShell()
    {
        if (_context.CurrentZone != null)
        {
            return;
        }

        var zone = _context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        _context.EnterZone(zone.ZoneDef);
    }

    private ArenaGui RecreateGui()
    {
        _gui?.Dispose();
        return new ArenaGui(_context, this, _worldTextHandler);
    }

    private void RecordMatchIfNeeded()
    {
        var run = _context.ArenaRun;
        if (_matchResultRecorded || run == null || _pendingResult == null || _lastPrepSnapshot == null)
        {
            return;
        }

        var localWon = !_context.PlayerPawn.IsDead;
        var serverWon = string.Equals(
            _pendingResult.WinnerPlayerId,
            run.PlayerId,
            StringComparison.Ordinal);
        var encounter = _context.CurrentZone?.ActiveEncounter;
        var handler = encounter?.CombatHandler;
        if (localWon != serverWon
            || (encounter is { Ticks: > 0 } && encounter.Ticks != _pendingResult.Ticks))
        {
            Log.Warning(DuelSimulator.DescribeMismatch(
                _pendingResult,
                localWon,
                run.PlayerId,
                encounter?.Ticks ?? 0,
                handler?.CauseOfDeath));
        }

        var learnedSkills = localWon == serverWon
            ? BuildSnapshotFactory.CaptureSkills(_context.PlayerPawn)
            : _lastPrepSnapshot.Skills;
        _recordedLoadout = _lastPrepSnapshot with { Skills = learnedSkills };
        run.RecordMatchResult(serverWon, _pendingResult.DefenderPlayerId ?? "unknown");
        if (!run.IsRunOver)
        {
            run.AssignNextMerchant();
        }

        _matchResultRecorded = true;
        SaveRun();
    }

    private void RestorePawnAfterCombat()
    {
        if (_lastPrepSnapshot == null)
        {
            return;
        }

        var loadout = _recordedLoadout ?? _lastPrepSnapshot;
        _context.RestoreArenaPawn();
        BuildSnapshotFactory.Apply(_context.PlayerPawn, loadout);
        EnsureZoneShell();
        _context.RefreshPlayerConsumableSlots();
    }

    private bool HasUnrecordedFinishedCombat()
    {
        return !_matchResultRecorded
               && _pendingResult != null
               && _lastPrepSnapshot != null
               && _context.ArenaRun?.Phase == ArenaPhase.Combat
               && _context.CurrentZone?.ActiveEncounter?.State == EncounterState.Finished;
    }

    public void SaveOnExit()
    {
        if (HasUnrecordedFinishedCombat())
        {
            RecordMatchIfNeeded();
        }
        else if (_context.ArenaRun != null && !_runFinishedOnServer)
        {
            SaveRun();
        }

        FlushPendingSave();
    }

    private void AutosavePrep(float deltaTime)
    {
        var phase = _context.ArenaRun?.Phase;
        if (phase is not (ArenaPhase.Prep or ArenaPhase.Shop or ArenaPhase.GeneralStore))
        {
            _autosaveTimer = 0;
            return;
        }

        _autosaveTimer += deltaTime;
        if (_autosaveTimer < 2f)
        {
            return;
        }

        _autosaveTimer = 0;
        SaveRun();
    }

    private void SaveRun()
    {
        if (_context.ArenaRun == null || _client == null || string.IsNullOrEmpty(_runId))
        {
            return;
        }

        var phase = _context.ArenaRun.Phase;
        var loadout = phase is ArenaPhase.Matching or ArenaPhase.Combat
            ? _recordedLoadout ?? _lastPrepSnapshot
            : _recordedLoadout;
        loadout ??= BuildSnapshotFactory.ToSnapshot(
            _context.PlayerPawn,
            _context.ArenaRun.PlayerId,
            buildId: $"arena-{Math.Max(1, _context.ArenaRun.FightsPlayed)}",
            seed: _context.RunSeed,
            round: Math.Max(1, _context.ArenaRun.FightsPlayed),
            rating: CurrentRank.Rating);
        var progress = ArenaProgressMapper.FromRun(_context.ArenaRun, loadout, _runId, _runStartedAt);
        if (_matchResultRecorded && _context.ArenaRun.Phase == ArenaPhase.Combat)
        {
            var nextPhase = _context.ArenaRun.IsRunOver ? ArenaPhase.RunEnd : ArenaPhase.MerchantSelect;
            progress = progress with { Phase = nextPhase.ToString() };
        }
        var achievements = ExportAchievements();

        var startWriter = false;
        lock (_saveLock)
        {
            _pendingProgress = progress;
            _pendingAchievements = achievements;
            _saveDirty = true;
            if (_saveBusy)
            {
                return;
            }

            _saveBusy = true;
            startWriter = true;
        }

        if (startWriter)
        {
            _saveTask = PersistPendingAsync();
        }
    }

    private async Task PersistPendingAsync()
    {
        while (true)
        {
            ArenaProgressRecord progress;
            AchievementState achievements;
            ArenaMatchClient client;
            lock (_saveLock)
            {
                if (!_saveDirty || _client == null)
                {
                    _saveBusy = false;
                    return;
                }

                _saveDirty = false;
                progress = _pendingProgress!;
                achievements = _pendingAchievements!;
                _pendingProgress = null;
                _pendingAchievements = null;
                client = _client;
            }

            try
            {
                await client.SaveCurrentArena(progress).ConfigureAwait(false);
                await client.SaveAchievements(progress.PlayerId, achievements).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MatchError = ex.Message;
                Log.Warning($"Arena server request failed: {ex.Message}");
            }
        }
    }

    private void FlushPendingSave()
    {
        try
        {
            FlushPendingSaveAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning($"Arena save flush failed: {ex.Message}");
        }
    }

    private async Task FlushPendingSaveAsync()
    {
        var task = _saveTask;
        if (task == null)
        {
            return;
        }

        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Log.Warning($"Arena save flush failed: {ex.Message}");
        }
    }

    private void FinishOnServer(bool victory)
    {
        FlushPendingSave();
        var finished = TryGet(() =>
        {
            var run = _client!.FinishArena(_profile.PlayerId, victory).GetAwaiter().GetResult();
            _client.SaveAchievements(_profile.PlayerId, ExportAchievements()).GetAwaiter().GetResult();
            RefreshRank();
            return run;
        });
        if (finished != null)
        {
            LastFinishedRun = finished;
            _runFinishedOnServer = true;
        }
    }

    private void RestoreProgress(ArenaProgressRecord current)
    {
        _context.InitializeArena(current.PlayerId, current.PlayerName, current.RunSeed);
        ArenaProgressMapper.ApplyTo(_context.ArenaRun!, current);
        _context.RefreshPlayerConsumableSlots();
        if (_context.ArenaRun!.Phase is ArenaPhase.Matching or ArenaPhase.Combat)
        {
            _context.ArenaRun.SetPhase(ArenaPhase.Prep);
        }
        else if (_context.ArenaRun.IsRunOver)
        {
            _context.ArenaRun.SetPhase(ArenaPhase.RunEnd);
        }

        if (current.Loadout != null)
        {
            BuildSnapshotFactory.Apply(_context.PlayerPawn, current.Loadout);
        }

        _runId = current.RunId;
        _runStartedAt = current.StartedAt;
    }

    private void RefreshRank()
    {
        ApplyRankFromProfile(TryGet(() => _client!.GetProfile(_profile.PlayerId).GetAwaiter().GetResult()));
    }

    private void ApplyRankFromProfile(PlayerProfileRecord? remote)
    {
        if (remote == null)
        {
            return;
        }

        CurrentRank = ArenaRank.FromRating(remote.Rating, remote.RatedRuns, remote.LegendNumber);
        _equippedNamePlate = string.IsNullOrWhiteSpace(remote.EquippedNamePlate)
            ? ArenaMarks.DefaultNamePlate
            : remote.EquippedNamePlate;
        ApplyEquippedNamePlate();
    }

    private void ApplyEquippedNamePlate()
    {
        var pawn = _context.World?.Player?.Pawn;
        if (pawn == null)
        {
            return;
        }

        pawn.NamePlateMoniker = string.IsNullOrWhiteSpace(_equippedNamePlate)
            ? ArenaMarks.DefaultNamePlate
            : _equippedNamePlate;
    }

    private void ImportAchievements(AchievementState? state)
    {
        if (state == null)
        {
            return;
        }

        foreach (var record in state.Achievements)
        {
            _context.Achievements.Import(
                record.Moniker,
                record.CurrentValue,
                record.IsUnlocked,
                record.UnlockedAt?.UtcDateTime,
                record.IsAcknowledged);
        }

        _context.RefreshPlayerConsumableSlots();
    }

    private AchievementState ExportAchievements()
    {
        return new AchievementState
        {
            Achievements = _context.Achievements.Export()
                .Select(progress => new AchievementRecord
                {
                    Moniker = progress.Def.Moniker,
                    CurrentValue = progress.CurrentValue,
                    IsUnlocked = progress.IsUnlocked,
                    UnlockedAt = progress.UnlockedAt is { } unlockedAt
                        ? new DateTimeOffset(DateTime.SpecifyKind(unlockedAt, DateTimeKind.Local)).ToUniversalTime()
                        : null,
                    IsAcknowledged = progress.IsAcknowledged
                })
                .ToList()
        };
    }

    private T? TryGet<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            MatchError = ex.Message;
            Log.Warning($"Arena server request failed: {ex.Message}");
            return default;
        }
    }

    private async Task<T?> TryGetAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            MatchError = ex.Message;
            Log.Warning($"Arena server request failed: {ex.Message}");
            return default;
        }
    }

    private static void WaitForBackground(Task? task)
    {
        if (task == null)
        {
            return;
        }

        try
        {
            task.GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    private sealed class ArenaLoadPayload
    {
        public bool IsStartFresh;
        public PlayerProfileRecord? EnsuredProfile;
        public PlayerProfileRecord? Profile;
        public ArenaProgressRecord? Current;
        public ArenaProgressRecord? Started;
        public AchievementState? Achievements;
    }

    private static void TryDeleteLocalSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }

    private void HandleInput()
    {
        var current = Keyboard.GetState();
        if (WasKeyJustPressed(Keys.OemTilde, current))
        {
            _gui?.ToggleConsole();
        }

        if (WasKeyJustPressed(Keys.Space, current))
        {
            _context.TogglePause();
        }

        _previousKeyboardState = current;
    }

    private bool WasKeyJustPressed(Keys key, KeyboardState current)
    {
        return current.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    }
}
