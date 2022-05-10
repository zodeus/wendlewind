using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim;

public class Zone {
    public ZoneDef Def;
    public int ZoneKills = 0;
    public int TotalZoneKills = 0;
    public float DistanceTraveled = 0;
    public float FurthestDistanceTraveled = 0;
    public Town? Town;
    public float Temperature = -1;

    public string Label => Def.Label;
    public ZoneType ZoneType => Def.ZoneType;

    public float PercentTraveled => DistanceTraveled / Def.TravelSize;
    public bool IsComplete => FurthestDistanceTraveled / Def.TravelSize >= 1;

    public void Reset() {
        ZoneKills = 0; // clear current zone kill count
        DistanceTraveled = 0;
    }

    public void Tick() {
        Town?.Tick();
    }
}

public class ZoneDef : Def {
    public ZoneType ZoneType = ZoneType.Invalid;
    public float TravelSize;
    public float TravelSpeedFactor = 1;
    public RangeInt MeanTimeBetweenEvents;
    public List<ZoneResourceRecord> Resources = new();
    public string? BackgroundTexturePath;
    private Texture2D? _texture;
    public virtual Texture2D BackgroundTexture => _texture ??= BackgroundTexturePath != null ? Core.Content.Load<Texture2D>(BackgroundTexturePath) : BaseContent.Textures.BadTexture;
}

public class ZoneResourceRecord {
    public ItemDef Item = null!;
    public RangeInt Amount;
    public float ChanceToHarvest = 1;
    public RangeFloat HarvestArea;
}