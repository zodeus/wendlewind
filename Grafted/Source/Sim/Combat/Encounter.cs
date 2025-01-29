namespace Grafted.Sim.Combat;

public class Encounter
{
    private EncounterState _state = EncounterState.NotStarted;
    public CombatHandler? CombatHandler { get; private set; }
    public event Action<EncounterState>? StateChangedAction;

    public Zone Zone;
    public EncounterDef Def = null!;
    public int Ticks;

    public readonly List<Pawn> PlayerPawns = [];
    public readonly List<Pawn> EnemyPawns = [];

    public Encounter(Zone zone)
    {
        Zone = zone;
    }

    public void Initialize()
    {
        if (Def.Enemies.Count > 0)
        {
            CombatHandler = new CombatHandler(this);
        }
    }

    public bool AtBoss => Def.IsBoss;

    public EncounterState State
    {
        get => _state;
        set
        {
            _state = value;
            StateChangedAction?.Invoke(_state);
        }
    }

    public void AddPlayerPawn(Pawn pawn)
    {
        PlayerPawns.Add(pawn);
    }

    public void AddEnemyPawn(Pawn pawn)
    {
        EnemyPawns.Add(pawn);
    }

    public void Tick()
    {
        Ticks++;
        CombatHandler?.Tick();
    }
}