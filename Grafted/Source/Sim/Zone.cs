using Grafted.Definitions;

namespace Grafted.Sim;

public class Zone {
    public ZoneDef Def;
    public int ZoneKills = 0;
    public int TotalZoneKills = 0;
    public float DistanceTraveled = 0;
    public float FurthestDistanceTraveled = 0;
    public Town? Town;

    public string Label => Def.Label;
    public ZoneType ZoneType => Def.ZoneType;

    public float PercentTraveled => DistanceTraveled / Def.TravelSize;

    public void Reset() {
        ZoneKills = 0; // clear current zone kill count
        if (DistanceTraveled > FurthestDistanceTraveled) {
            FurthestDistanceTraveled = DistanceTraveled;
        }

        DistanceTraveled = 0;
    }
}

public class ZoneDef : Def {
    public ZoneType ZoneType = ZoneType.Invalid;
    public float TravelSize;
}