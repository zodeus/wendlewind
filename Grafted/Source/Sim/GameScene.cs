using Grafted.Scenes;
using Grafted.Sim.Gui.CombatGuis;

namespace Grafted.Sim;

public class GameScene : Scene {
    private CameraController _cameraController = null!;

    protected override void OnStart() {
        _cameraController = new CameraController(MainCamera);
        Core.CameraController = _cameraController;
        Core.Sim = new Simulation();
        QuickPlay();
    }

    public void QuickPlay() {
        Core.ClearCoroutines();
        Core.Sim.World = WorldGenerator.GenerateNewWorld();
        Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
    }

    public override void Update(float deltaTime) {
        Core.Sim.Update(deltaTime);

        _cameraController.Update(deltaTime);
    }

    public override void FixedUpdate() {
        Core.Sim.FixedUpdate();
    }

    public override void Draw(float deltaTime) {
        Core.Sim.Draw(Core.Graphics.Batcher, deltaTime);
    }
}