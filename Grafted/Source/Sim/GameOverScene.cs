using Grafted.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
namespace Grafted.Sim;

public class GameOverScene : Scene {
    private CameraController _cameraController = null!;

    protected override void OnStart() {
        Core.Schedule(2, _ => {
            Core.Scene.Load<GameScene>();
        });
    }

    public override void Update(float deltaTime) { }

    public override void Draw(float deltaTime) {
        var spriteBatch = Core.Graphics.Batcher;
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Core.Scene.MainCamera.View()
        );
        spriteBatch.DrawString(BaseContent.Fonts.Default.Large, "GAME OVER", new Vector2(0, 0), Color.White);
        spriteBatch.DrawString(BaseContent.Fonts.Default.Normal, "Starting new game...", new Vector2(0, 50), Color.White);
        spriteBatch.End();
    }
}