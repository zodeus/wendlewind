using System.IO;
using System.Linq;
using Grafted.Debug;
using Grafted.Definitions;
using Grafted.Scenes;
using Grafted.Sim.Entities.Items;
using Grafted.Utils;

namespace Grafted.Sim;

public class GameScene : Scene {
    private CameraController _cameraController = null!;

    protected override void OnStart() {
        _cameraController = new CameraController(MainCamera);
        Core.CameraController = _cameraController;
        Core.Sim = new Simulation();
        if (DebugSettings.QuickLoad && File.Exists("save.xml")) {
            Core.Sim.Load("save.xml");
        }
        else {
            QuickPlay();
        }
    }

    public void QuickPlay() {
        Core.ClearCoroutines();
        Core.Sim.World = WorldGenerator.GenerateNewWorld(Defs.PawnConfigs.PlayerPawn);
        Core.Sim.ChangeZone(Defs.Zones.VillageOfTheDamned, false);
        //DebugInfo();
    }

    private void DebugInfo() {
        foreach (ItemDef item in DefRepository<ItemDef>.Defs) {
            Log.Info($"{item.Label}: {item.BaseStats.Where(stat => stat.Def == Defs.Stats.CurrencyValue).FirstOrNull()?.Value}");
        }
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