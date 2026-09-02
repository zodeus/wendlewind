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
    private Task? _matchTask;
    private readonly object _saveLock = new();
    private Task? _saveTask;
    private bool _saveBusy;
    private bool _saveDirty;
    private ArenaProgressRecord? _pendingProgress;
    private AchievementState? _pendingAchievements;
    private KeyboardState _previousKeyboardState;
    private float _autosaveTimer;

    public string? MatchError { get; private set; }
    public ArenaRankDisplay CurrentRank { get; private set; } = ArenaRank.FromRating(ArenaRank.StartingRating, 0);
    public ArenaRunRecord? LastFinishedRun { get; private set; }
    public CombatResult? LastCombatResult => _pendingResult;

    protected override void OnStart()
    {
        MatchError = null;
        _pendingResult = null;
        _matchTask = null;
        _lastPrepSnapshot = null;
        _profile = PlayerProfile.LoadOrCreate();
        _context = _runScope.ServiceProvider.GetRequiredService<GameContext>();
        Core.Context = _context;
        _client = new ArenaMatchClient();

        TryDeleteLocalSave();
        SyncProfile();

        var startFresh = StartFresh;
        StartFresh = false;
        var current = TryGet(() => _client.GetCurrentArena(_profile.PlayerId).GetAwaiter().GetResult());

        if (!startFresh && current != null)
        {
            RestoreProgress(current);
        }
        else
        {
            var reuseSeed = startFresh && current is { RunSeed: > 0 } ? current.RunSeed : 0;
            var started = TryGet(() => _client.StartArena(
                _profile.PlayerId,
                _profile.DisplayName,
                reuseSeed).GetAwaiter().GetResult());
            var runSeed = started?.RunSeed > 0 ? started.RunSeed : Random.Shared.Next();
            _context.InitializeArena(_profile.PlayerId, _profile.DisplayName, runSeed);
            _runId = started?.RunId ?? Guid.NewGuid().ToString("N");
            _runStartedAt = started?.StartedAt ?? DateTimeOffset.UtcNow;
            SaveRun();
        }

        ApplyServerAchievements();
        _gui = new ArenaGui(_context, this, _worldTextHandler);
    }

    public override void End()
    {
        SaveOnExit();
        _gui?.Dispose();
        _gui = null;
        _client?.Dispose();
        _client = null;
        _worldTextHandler.Clear();
    }

    public override void Update(float deltaTime)
    {
        HandleInput();
        PollMatch();
        AutosavePrep(deltaTime);
        _gui?.Update(deltaTime);
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
        _gui?.Draw(Core.Graphics.Batcher, deltaTime);
        _worldTextHandler.Render(Core.Graphics.Batcher, deltaTime);
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
        _matchTask = Task.Run(() => RequestMatch(snapshot));
    }

    public void ReturnToPrep()
    {
        MatchError = null;
        _context.ArenaRun?.SetPhase(ArenaPhase.Prep);
    }

    public void OnVisualCombatFinished()
    {
        var run = _context.ArenaRun;
        if (run == null || _pendingResult == null || _lastPrepSnapshot == null)
        {
            run?.SetPhase(ArenaPhase.Prep);
            return;
        }

        var localWon = !_context.PlayerPawn.IsDead;
        var serverWon = string.Equals(
            _pendingResult.WinnerPlayerId,
            run.PlayerId,
            StringComparison.Ordinal);
        if (localWon != serverWon)
        {
            Log.Warning(
                $"Arena re-sim disagreed with server. LocalWon={localWon} ServerWinner={_pendingResult.WinnerPlayerId}");
        }

        var learnedSkills = BuildSnapshotFactory.CaptureSkills(_context.PlayerPawn);
        _context.RestoreArenaPawn();
        BuildSnapshotFactory.Apply(_context.PlayerPawn, _lastPrepSnapshot with { Skills = learnedSkills });
        EnsureZoneShell();
        run.ApplyMatchResult(serverWon, _pendingResult.DefenderPlayerId ?? "unknown");
        SaveRun();
        if (run.IsRunOver)
        {
            FinishOnServer(run.IsVictory);
            _gui = RecreateGui();
        }
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
        if (_context.ArenaRun?.IsRunOver == true)
        {
            FinishOnServer(_context.ArenaRun.IsVictory);
        }
        else
        {
            SaveRun();
        }

        Core.ChangeScene<MainMenuScene>();
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

    public void SaveOnExit()
    {
        if (_context.ArenaRun is { IsRunOver: false })
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

        var loadout = BuildSnapshotFactory.ToSnapshot(
            _context.PlayerPawn,
            _context.ArenaRun.PlayerId,
            buildId: $"arena-{Math.Max(1, _context.ArenaRun.FightsPlayed)}",
            seed: _context.RunSeed,
            round: Math.Max(1, _context.ArenaRun.FightsPlayed),
            rating: CurrentRank.Rating);
        var progress = ArenaProgressMapper.FromRun(_context.ArenaRun, loadout, _runId, _runStartedAt);
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
        var task = _saveTask;
        if (task == null)
        {
            return;
        }

        try
        {
            task.GetAwaiter().GetResult();
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
        }
    }

    private void RestoreProgress(ArenaProgressRecord current)
    {
        _context.InitializeArena(current.PlayerId, current.PlayerName, current.RunSeed);
        ArenaProgressMapper.ApplyTo(_context.ArenaRun!, current);
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

    private void SyncProfile()
    {
        var remote = TryGet(() => _client!.EnsureProfile(_profile.PlayerId, _profile.DisplayName).GetAwaiter().GetResult());
        if (remote != null && !string.IsNullOrWhiteSpace(remote.DisplayName))
        {
            _profile.DisplayName = remote.DisplayName;
        }

        RefreshRank();
    }

    private void RefreshRank()
    {
        var remote = TryGet(() => _client!.GetProfile(_profile.PlayerId).GetAwaiter().GetResult());
        if (remote == null)
        {
            return;
        }

        CurrentRank = ArenaRank.FromRating(remote.Rating, remote.RatedRuns, remote.LegendNumber);
    }

    private void ApplyServerAchievements()
    {
        var state = TryGet(() => _client!.GetAchievements(_profile.PlayerId).GetAwaiter().GetResult());
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
