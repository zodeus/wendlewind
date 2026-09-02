namespace Wendlemire.Sim;

public enum GameState
{
    Map,
    Zone,
    StartOver
}

public class GameContext : IExposable, IHasContext
{
    public const int TicksPerSecond = 60;

    private Random _rng = new();
    private IRng _simRng = new SimRng(new Random());

    GameContext IHasContext.Context
    {
        get => this;
        set { }
    }

    /// <summary>
    /// Identity of this run. Encounter seeds are derived from this, zone, and stage.
    /// </summary>
    public int RunSeed { get; private set; }

    /// <summary>
    /// Sim RNG owned by this context.
    /// </summary>
    public Random Rng
    {
        get => _rng;
        set
        {
            _rng = value;
            _simRng = new SimRng(value);
        }
    }

    public IRng SimRng => _simRng;

    public ISimFactory Factory { get; private set; }

    public IdProvider IdProvider = new();
    public PlayerKillRecords DeathRecords = null!;
    public AchievementTracker Achievements = null!;
    public World World = null!;
    public bool IsPaused = false;
    public int Ticks;
    public Player Player => World.Player;
    public Pawn PlayerPawn => World.Player.Pawn;
    public event Action<GameState>? OnStateChanged;
    public Zone? CurrentZone { get; set; }
    public ArenaRun? ArenaRun;

    public GameContext()
    {
        Factory = new SimFactory(this);
        DeathRecords = new PlayerKillRecords();
        Achievements = new AchievementTracker();
    }

    public void AttachServices(IServiceProvider services)
    {
        var factory = new SimFactory(this, services);
        Factory = factory;
        Scribe.ObjectFactory = factory;
    }

    public void Initialize(int? runSeed = null, string? playerName = null)
    {
        RunSeed = runSeed ?? System.Random.Shared.Next();
        Rng = new Random(RunSeed);
        World = WorldGenerator.GenerateNewWorld(this, playerName);
        CurrentZone = null;
        ArenaRun = null;
        Ticks = 0;
        Achievements = new AchievementTracker();
        Achievements.Context = this;
        Achievements.Initialize();
        Factory.RebindGraph();
        WireUpEvents();
        RefreshPlayerConsumableSlots();
    }

    public void InitializeArena(string playerId, string playerName, int? runSeed = null)
    {
        Initialize(runSeed, playerName);
        Player.ResetForArena(playerName);
        WirePawnEvents();
        ArenaRun = new ArenaRun();
        ArenaRun.Start(playerId, playerName, RunSeed);
        RefreshPlayerConsumableSlots();
    }

    public void RestoreArenaPawn()
    {
        if (ArenaRun == null)
        {
            return;
        }

        Player.ResetForArena(ArenaRun.PlayerName);
        WirePawnEvents();
        RefreshPlayerConsumableSlots();
    }

    public void Tick()
    {
        if (IsPaused)
        {
            return;
        }

        if (CurrentZone?.State != ZoneState.Combat)
        {
            return;
        }

        if (CurrentZone?.ActiveEncounter?.State != EncounterState.InProgress)
        {
            return;
        }

        InternalTick();
    }

    private void InternalTick()
    {
        Ticks++;
        World.Player.Pawn.Tick();
        CurrentZone?.Tick();
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void EnterZone(ZoneDef zoneDef)
    {
        CurrentZone = World.Zones.First(z => z.ZoneDef == zoneDef);
        CurrentZone!.OnStateChanged += ZoneStageChanged;
        CurrentZone.Enter(Player);
        ChangeGameState(GameState.Zone);
    }

    public void StartOver()
    {
        Ticks = 0;
        CurrentZone = null;
        DeathRecords.Reset();
        World.Reset();
        Player.Reset();
        WireUpEvents();
        Achievements.OnWorldRestart(this);
        RefreshPlayerConsumableSlots();
        if (CurrentZone != null)
        {
            CurrentZone.OnStateChanged -= ZoneStageChanged;
        }
        ChangeGameState(GameState.StartOver);
    }

    private void ZoneStageChanged(ZoneState zoneState)
    {
        if (zoneState != ZoneState.Exit) return;

        CurrentZone!.OnStateChanged -= ZoneStageChanged;
        ChangeGameState(GameState.Map);
    }

    private void ChangeGameState(GameState value)
    {
        OnStateChanged?.Invoke(value);
        if (value == GameState.Map)
        {
            Save();
        }
    }

    #region Persistence

    public void Save(string filePath = "save.xml")
    {
        Log.Info("Saving Game to " + filePath);
        Scribe.ObjectFactory = Factory;
        Scribe.Saver.InitSaving(filePath, "SaveData");
        var context = this;
        ScribeDeep.Look(ref context!, "Context");
        Scribe.Saver.FinalizeSaving();
    }

    public void Load(string filePath)
    {
        CurrentZone = null;
        Scribe.ObjectFactory = Factory;
        Scribe.Loader.InitLoading(filePath);
        if (!Scribe.EnterNode("Context"))
        {
            Log.Error("Could not find game XML node.");
            Scribe.ForceStop();
            return;
        }

        ExposeDataInternal();
        Scribe.Loader.FinalizeLoading();

        Factory.RebindGraph();
        Achievements.Context = this;
        Achievements.Initialize();
        WireUpEvents();
        RefreshPlayerConsumableSlots();
    }

    private void WireUpEvents()
    {
        WirePawnEvents();
    }

    private void WirePawnEvents()
    {
        Player.Pawn.FoodConsumed += (p, i) => Achievements.OnItemUsed(p, i, null);
        Player.Pawn.DamageTaken += Achievements.OnPlayerDamaged;
        Player.Pawn.Inventory.ItemAdded += Achievements.OnItemFound;
        Player.Pawn.Inventory.ItemAdded += Player.OnItemFound;
    }

    public int PrepUnlockRound =>
        ArenaRun == null
            ? PrepSlotUnlocks.FullyUnlockedRound
            : Math.Max(1, ArenaRun.FightsPlayed + 1);

    public void RefreshPlayerConsumableSlots()
    {
        PlayerPawn.RefreshConsumableSlots(PrepUnlockRound);
    }

    public void ExposeData()
    {
        if (Scribe.State == ScribeState.LoadingObjects)
        {
            Log.Error("You must use Simulation.Load method to load simulation.");
            return;
        }

        ExposeDataInternal();
    }

    private void ExposeDataInternal()
    {
        ScribeDeep.Look(ref World!, "World");
        ScribeDeep.Look(ref IdProvider!, "IdProvider");
        ScribeValues.Look(ref Ticks, "Ticks");
        var runSeed = RunSeed;
        ScribeValues.Look(ref runSeed, "RunSeed");
        RunSeed = runSeed;
        if (Scribe.State != ScribeState.Saving && RunSeed != 0)
        {
            Rng = new Random(RunSeed);
        }
        ScribeDeep.Look(ref DeathRecords!, "DeathRecords");
        ScribeDeep.Look(ref Achievements!, "Achievements");
        ScribeDeep.Look(ref ArenaRun, "ArenaRun");
    }

    #endregion
}
