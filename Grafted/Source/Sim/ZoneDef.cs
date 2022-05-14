using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Gui.SpecialEvents;
using Grafted.Sim.SpecialEvents;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim;

public class ZoneDef : Def {
    private Texture2D? _texture;

    public ZoneType ZoneType = ZoneType.Invalid;
    public Type EventHandlerClass = typeof(SpecialEventHandler);
    public Type EventGuiClass = typeof(SpecialEventGui);
    public string? BackgroundTexturePath;
    public int BackgroundTextureTransparency = 20;
    public Point Location = Point.Zero;

    // Combat Zone Settings
    public float TravelSize;
    public float TravelSpeedFactor = 1;
    public RangeInt MeanTimeBetweenEvents;
    public List<ZoneResourceRecord> Resources = new();

    public virtual Texture2D BackgroundTexture => _texture ??= BackgroundTexturePath != null ? Core.Content.Load<Texture2D>(BackgroundTexturePath) : BaseContent.Textures.BadTexture;
    public SpecialEventHandler Handler => (SpecialEventHandler) Activator.CreateInstance(EventHandlerClass)!;
    public SpecialEventGui Gui => (SpecialEventGui) Activator.CreateInstance(EventGuiClass, Handler)!;
}