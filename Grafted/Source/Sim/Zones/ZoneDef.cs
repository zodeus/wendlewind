using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Gui.Zones;
using Grafted.Sim.Zones.Handlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Zones;

public class ZoneDef : Def {
    private Texture2D? _texture;

    public ZoneType ZoneType = ZoneType.Invalid;
    public Type HandlerClass = typeof(ZoneHandler);
    public Type GuiClass = typeof(ZoneGui);
    public string? BackgroundTexturePath;
    public int BackgroundTextureTransparency = 20;
    public Point Location = Point.Zero;

    // Combat Zone Settings
    public float TravelSize;
    public float TravelSpeedFactor = 1;
    public RangeInt MeanTimeBetweenEvents;
    public List<ZoneResourceRecord> Resources = new();

    public virtual Texture2D BackgroundTexture => _texture ??= BackgroundTexturePath != null ? Core.Content.Load<Texture2D>(BackgroundTexturePath) : BaseContent.Textures.BadTexture;
    public ZoneHandler Handler => (ZoneHandler) Activator.CreateInstance(HandlerClass)!;
    public ZoneGui Gui => (ZoneGui) Activator.CreateInstance(GuiClass)!;
}