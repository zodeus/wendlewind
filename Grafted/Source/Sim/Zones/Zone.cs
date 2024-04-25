using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Zones;

public enum ZoneState
{
    Map,
    Combat,
    CombatResults,
    Unoccupied
}

public class Zone : IExposable, IIdentityProvider
{
    public BiomeDef BiomeDef = null!;
    public int ZoneKills = 0;

    public float Temperature = -1;
    public bool IsComplete;
    public string Label => BiomeDef.Label;
    public ZoneState State { get; set; }
    public Encounter? ActiveEncounter { get; set; }
    public Player? Player { get; set; }
    public event Action<ZoneState>? OnStateChanged;
    public event Action<ScreenMessageData>? OnZoneMessage;

    public void Tick(int ticks)
    {
        ActiveEncounter?.Tick(ticks);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref BiomeDef!, "BiomeDef");
        Scribe_Values.Look(ref IsComplete, "IsComplete");
        Scribe_Values.Look(ref ZoneKills, "ZoneKills");
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
        ChangeState(ZoneState.Map);
    }

    public void NextEncounter()
    {
        ActiveEncounter = CombatGenerator.GenerateForZone(Player!.Pawn, this);
        if (ActiveEncounter.AtBoss)
        {
            Alert(new ScreenMessageData
            {
                Color = Color.LightGoldenrodYellow, Duration = 5,
                Font = BaseContent.Fonts.Fancy.Huge,
                Text = "BOSS FIGHT!"
            });
        }

        ChangeState(ZoneState.Combat);
        ActiveEncounter.State = EncounterState.InProgress;
    }

    public void Alert(ScreenMessageData message)
    {
        OnZoneMessage?.Invoke(message);
    }

    public void CombatResults()
    {
        ZoneKills++;
        ChangeState(ZoneState.CombatResults);
    }

    public void Exit()
    {
        Player = null;
        ChangeState(ZoneState.Unoccupied);
        
    }

    private void ChangeState(ZoneState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }
}