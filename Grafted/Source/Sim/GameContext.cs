namespace Grafted.Sim;

public enum GameState
{
    Map,
    Zone,
    StartOver
}

public class GameContext : IExposable
{
    //public GameMessages Messages = new();
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

    public GameContext()
    {
        DeathRecords = new PlayerKillRecords();
        Achievements = new AchievementTracker();
    }

    public void Initialize()
    {
        World = WorldGenerator.GenerateNewWorld();
        CurrentZone = null;
        Ticks = 0;
        Achievements = new AchievementTracker();
        Achievements.Initialize();
        WireUpEvents();
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

    public void TickOnce()
    {
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
        //Save();
        if (CurrentZone != null)
        {
            CurrentZone.OnStateChanged -= ZoneStageChanged;
        }
        ChangeGameState(GameState.StartOver);
    }

    private void ZoneStageChanged(ZoneState zoneState)
    {
        if (zoneState != ZoneState.Exit) return;

        // Return to camp
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
        // Log.Warning("Save is dislabled");
        //return;
        Log.Info("Saving Game to " + filePath);
        Scribe.Saver.InitSaving(filePath, "SaveData");
        var context = this;
        ScribeDeep.Look(ref context!, "Context");
        Scribe.Saver.FinalizeSaving();
    }

    public void Load(string filePath)
    {
        CurrentZone = null;
        Scribe.Loader.InitLoading(filePath);
        if (!Scribe.EnterNode("Context"))
        {
            Log.Error("Could not find game XML node.");
            Scribe.ForceStop();
            return;
        }

        ExposeDataInternal();
        Scribe.Loader.FinalizeLoading();

        WireUpEvents();
    }

    private void WireUpEvents()
    {
        Player.Pawn.FoodConsumed += Achievements.OnItemUsed;
        Player.Pawn.DamageTaken += Achievements.OnPlayerDamaged;
        Player.Pawn.Inventory.ItemAdded += Achievements.OnItemFound;
        Player.Pawn.Inventory.ItemAdded += Player.OnItemFound;
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
        ScribeDeep.Look(ref DeathRecords!, "DeathRecords");
        ScribeDeep.Look(ref Achievements!, "Achievements");
    }

    #endregion
}