using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Persistence;
using Grafted.Sim.SpecialEvents;

namespace Grafted.Sim;

public class Zone : IExposable, IIdentityProvider {
    public ZoneDef Def = null!;
    public int ZoneKills = 0;
    public int TotalZoneKills = 0;
    public float DistanceTraveledThisRun = 0;
    public bool BossKilledThisRun;

    public float FurthestDistanceTraveled = 0;
    public Town? Town;
    public float Temperature = -1;
    public bool IsComplete;
    private SpecialEventHandler? Handler;

    public string Label => Def.Label;
    public ZoneType ZoneType => Def.ZoneType;
    public float PercentTraveled => DistanceTraveledThisRun / Def.TravelSize;

    public void Reset() {
        ZoneKills = 0; // clear current zone kill count
        DistanceTraveledThisRun = 0;
        BossKilledThisRun = false;
    }

    public void Tick() {
        Town?.Tick();
    }

    public void ExposeData() {
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_Values.Look(ref TotalZoneKills!, "TotalZoneKills");
        Scribe_Values.Look(ref FurthestDistanceTraveled!, "FurthestDistanceTraveled");
        Scribe_Values.Look(ref IsComplete, "IsComplete");
        Scribe_Deep.Look(ref Town!, "Town");
    }

    public string GetUniqueId() {
        return Def.Moniker;
    }

    public void Initialize() {
        if (ZoneType == ZoneType.Town) {
            Town = TownGenerator.Generate(Def);
        }

        //andler = Def.Handler;
        //Gui = Def.Gui;
    }
}

public class ZoneResourceRecord {
    public ItemDef Item = null!;
    public RangeInt Amount;
    public float ChanceToHarvest = 1;
    public RangeFloat HarvestArea;
}