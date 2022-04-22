using Grafted.Debug;
using Grafted.Definitions;
using Grafted.Scenes;
using Grafted.Sim.Gui;

namespace Grafted.Sim;

public class GameScene : Scene {
    private CameraController _cameraController = null!;
    private bool _firstTime = true;

    protected override void OnStart() {
        _cameraController = new CameraController(MainCamera);
        Core.CameraController = _cameraController;
        Core.Sim = new Simulation();
        QuickPlay();
    }

    public void QuickPlay() {
        Core.ClearCoroutines();
        Core.Sim.World = WorldGenerator.GenerateNewWorld();

        if (DebugSettings.SkipIntro) {
            //skip intro
            Core.Sim.World.CurrentZone.Def = Defs.Zones.VillageOfTheDamned;
            Core.Sim.Gui = new TownGui(Core.Sim.World.Zones[Defs.Zones.VillageOfTheDamned].Town!);
            return;
        }

        if (_firstTime && DebugSettings.SkipIntroDialogue == false) {
            _firstTime = false;
            Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue());
        }
        else {
            Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
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