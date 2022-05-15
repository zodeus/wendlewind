using Grafted.Sim.Combat;
using Grafted.Sim.Gui.Zones;
using Grafted.Sim.Persistence;
using Grafted.Sim.Zones.Handlers;

namespace Grafted.Sim.Zones;

public class Zone : IExposable, IIdentityProvider {
    private ZoneHandler _handler = null!;
    private ZoneGui _gui;

    public ZoneDef Def = null!;
    public World World = null!;
    public int ZoneKills = 0;
    public int TotalZoneKills = 0;
    public float DistanceTraveledThisRun = 0;
    public bool BossKilledThisRun;

    public float FurthestDistanceTraveled = 0;
    public float Temperature = -1;
    public bool IsComplete;
    public string Label => Def.Label;
    public ZoneGui Gui => _gui;
    public ZoneHandler Handler => _handler;
    public Town? Town => _handler as Town;
    public Adventure? Adventure => _handler as Adventure;
    public ZoneType ZoneType => Def.ZoneType;
    public float PercentTraveledThisRun => DistanceTraveledThisRun / Def.TravelSize;
    public float PercentTraveled => FurthestDistanceTraveled / Def.TravelSize;

    public void Reset() {
        ZoneKills = 0; // clear current zone kill count
        DistanceTraveledThisRun = 0;
        BossKilledThisRun = false;
    }

    public void Tick() {
        _handler?.Tick();
    }

    public void ExposeData() {
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_References.Look(ref World!, "World");
        Scribe_Values.Look(ref TotalZoneKills!, "TotalZoneKills");
        Scribe_Values.Look(ref FurthestDistanceTraveled!, "FurthestDistanceTraveled");
        Scribe_Values.Look(ref IsComplete, "IsComplete");
        Scribe_Deep.Look(ref _handler!, "Handler");
    }

    public string GetUniqueId() {
        return Def.Moniker;
    }

    public void Initialize(World world, ZoneDef zoneDef) {
        Def = zoneDef;
        World = world;
        _handler = zoneDef.Handler;
        _handler.Initialize(world, this);
    }

    public void Enter() {
        _gui = Def.Gui;
        _gui.Initialize(this);
        //ProgressTime(SimTime.SecondsInMinute * minutesSpentTravelling);
        Handler.OnEnter();
    }

    public void Exit() {
        //ProgressTime(SimTime.SecondsInMinute * minutesSpentTravelling);
        Handler.OnExit();
    }
}