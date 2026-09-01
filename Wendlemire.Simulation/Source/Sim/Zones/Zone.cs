
namespace Wendlemire.Sim.Zones;

public enum ZoneState
{
    Preparation,
    Mystery,
    Combat,
    CombatResults,
    Exit
}

public class Zone : IExposable, IIdentityProvider, IHasContext
{
    public GameContext Context { get; set; } = null!;
    public ZoneDef ZoneDef = null!;
    public int Stage;
    public bool IsComplete;
    public ZoneState State { get; set; }
    public Encounter? ActiveEncounter { get; set; }
    public Player? Player { get; set; }
    public event Action<ZoneState>? OnStateChanged;
    public event Action<ScreenMessageData>? OnZoneMessage;

    public void Tick()
    {
        ActiveEncounter?.Tick();
    }

    public void ExposeData()
    {
        ScribeDefs.Look(ref ZoneDef!, "ZoneDef");
        ScribeValues.Look(ref IsComplete, "IsComplete");
        ScribeValues.Look(ref Stage, "Stage");
    }

    public string GetUniqueId()
    {
        return ZoneDef.Moniker;
    }

    public void Initialize(ZoneDef zoneDef)
    {
        ZoneDef = zoneDef;
    }

    public void Enter(Player player)
    {
        Player = player;
        State = ZoneState.Preparation;
    }

    public void NextEncounter()
    {
        ActiveEncounter?.Dispose();
        ActiveEncounter = CombatGenerator.GenerateForZone(Context, Player!.Pawn, this);
        if (ActiveEncounter.AtBoss)
        {
            Alert(new ScreenMessageData
            {
                Color = Color.LightGoldenrodYellow, Duration = 5,
                Text = "BOSS FIGHT!"
            });
        }

        if (ActiveEncounter.CombatHandler != null)
        {
            ChangeState(ZoneState.Combat);
        }
        else if (ActiveEncounter.Def.MysteryProperties != null)
        {
            ChangeState(ZoneState.Mystery);
        }

        ActiveEncounter.State = EncounterState.InProgress;
    }

    public void StartHumanDuel(Pawn player, Pawn enemy, int? seed = null)
    {
        ActiveEncounter?.Dispose();
        ActiveEncounter = CombatGenerator.GenerateHumanDuel(Context, player, enemy, this, seed);
        ChangeState(ZoneState.Combat);
        ActiveEncounter.State = EncounterState.InProgress;
    }

    public void ReturnToPreparation()
    {
        ActiveEncounter?.Dispose();
        ActiveEncounter = null;
        ChangeState(ZoneState.Preparation);
    }

    public void Alert(ScreenMessageData message)
    {
        OnZoneMessage?.Invoke(message);
    }

    public void CombatResults()
    {
        Stage++;
        ChangeState(ZoneState.CombatResults);
    }

    public void Exit()
    {
        Player = null;
        ChangeState(ZoneState.Exit);
    }

    private void ChangeState(ZoneState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }
}