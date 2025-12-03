using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Zones;

public enum ZoneState
{
    Shrine,
    Combat,
    CombatResults,
    Exit
}

public class Zone : IExposable, IIdentityProvider
{
    public BiomeDef BiomeDef = null!;
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
        ScribeDefs.Look(ref BiomeDef!, "BiomeDef");
        ScribeValues.Look(ref IsComplete, "IsComplete");
        ScribeValues.Look(ref Stage, "Stage");
    }

    public string GetUniqueId()
    {
        return BiomeDef.Moniker;
    }

    public void Initialize(BiomeDef biomeDef)
    {
        BiomeDef = biomeDef;
    }

    public void Enter(Player player)
    {
        Player = player;
    }

    public void NextEncounter()
    {
        ActiveEncounter?.Dispose();
        ActiveEncounter = CombatGenerator.GenerateForZone(Player!.Pawn, this);
        if (ActiveEncounter.AtBoss)
        {
            Alert(new ScreenMessageData
            {
                Color = Color.LightGoldenrodYellow, Duration = 5,
                Font = BaseContent.Fonts.Default.Huge,
                Text = "BOSS FIGHT!"
            });
        }

        if (ActiveEncounter.CombatHandler != null)
        {
            ChangeState(ZoneState.Combat);
        }
        else if (ActiveEncounter.Def.ShrineProperties != null)
        {
            ChangeState(ZoneState.Shrine);
        }

        ActiveEncounter.State = EncounterState.InProgress;
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