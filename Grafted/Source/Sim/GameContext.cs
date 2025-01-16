namespace Grafted.Sim;

public enum GameState
{
    Camp,
    Zone,
    Restart
}

public class GameContext : IExposable
{
    public GameMessages Messages = new();
    public IdProvider IdProvider = new();
    public PlayerKillRecords DeathRecords = null!;
    public OminousMessageSpawner OminousMessageSpawner = null!;
    public World World = null!;
    public bool IsPaused = true;
    public int Ticks;
    public Player Player => World.Player;
    public Pawn PlayerPawn => World.Player.Pawn;
    public event Action<GameState>? OnStateChanged;
    public Zone? CurrentZone { get; set; }

    public GameContext()
    {
        DeathRecords = new PlayerKillRecords();
        OminousMessageSpawner = new OminousMessageSpawner();
    }

    public void Tick()
    {
        if (IsPaused)
        {
            return;
        }

        if (CurrentZone?.ActiveEncounter?.Zone?.State != ZoneState.Combat)
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
        OminousMessageSpawner.Tick();
        World.Player.Pawn.Tick();
        CurrentZone?.Tick();
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void EnterZone(BiomeDef biome)
    {
        CurrentZone = World.Zones.First(z => z.BiomeDef == biome);
        CurrentZone!.OnStateChanged += ZoneStageChanged;
        CurrentZone.Enter(Player);
        CurrentZone.NextEncounter();
        ChangeState(GameState.Zone);
    }

    public void ReturnToCamp()
    {
        CurrentZone!.OnStateChanged -= ZoneStageChanged;
        ChangeState(GameState.Camp);
    }

    public void Restart()
    {
        CurrentZone!.OnStateChanged -= ZoneStageChanged;
        ChangeState(GameState.Restart);
    }

    private void ZoneStageChanged(ZoneState zoneState)
    {
        if (zoneState == ZoneState.Unoccupied)
        {
            ReturnToCamp();
        }
    }

    private void ChangeState(GameState value)
    {
        OnStateChanged?.Invoke(value);
        //Save("save.xml");
        Log.Info("Autosaving disabled");
    }

    #region Persistence

    public void Save(string filePath)
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
        ScribeDeep.Look(ref Messages!, "Messages");
        ScribeValues.Look(ref Ticks!, "Ticks");
        ScribeDeep.Look(ref OminousMessageSpawner!, "OminousMessageSpawner");
        ScribeDeep.Look(ref DeathRecords!, "DeathRecords");
    }

    #endregion
}