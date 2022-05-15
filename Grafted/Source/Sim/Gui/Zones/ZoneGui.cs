using Grafted.Sim.Zones;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Gui.Zones;

public abstract class ZoneGui : BaseGui {
    public Zone Zone { get; set; } = null!;

    public ZoneGui() { }

    public virtual void Initialize(Zone zone) {
        Zone = zone;
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        spriteBatch.Draw(Core.Sim.World.CurrentZone.Def.BackgroundTexture, new Rectangle(0, 0, Screen.Width, Screen.Height), new Color(255, 255, 255, Core.Sim.World.CurrentZone.Def.BackgroundTextureTransparency));
        spriteBatch.End();

        base.Render(spriteBatch, deltaTime);
    }
}