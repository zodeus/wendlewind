using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Grafted.Debug;
using Grafted.Definitions;
using Grafted.Scenes;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui;
using Grafted.Utils;
using SharpDX.MediaFoundation;

namespace Grafted.Sim;

public class GameScene : Scene {
    private CameraController _cameraController = null!;
    private bool _firstTime = true;

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

        Core.Sim.World.MoveToZone(Defs.Zones.VillageOfTheDamned, false);
        Core.Sim.Gui = new TownGui(Core.Sim.World.Zones[Defs.Zones.VillageOfTheDamned].Town!);

        //DebugInfo();
    }

    private void DebugInfo() {
        foreach (ItemDef item in DefRepository<ItemDef>.Defs) {
            Log.Info($"{item.Label}: {item.BaseStats.Where(stat => stat.Def == Defs.Stats.CurrencyValue).FirstOrNull()?.Value}");
        }
    }

    public void PlayIntro() {
        Core.ClearCoroutines();
        Core.Sim.World = WorldGenerator.GenerateNewWorld(Defs.PawnConfigs.IntroPlayerPawn);
        if (_firstTime && DebugSettings.SkipIntroDialogue == false) {
            _firstTime = false;
            Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue());
        }
        else {
            Core.Sim.World.MoveToZone(Defs.Zones.Intro, false);
            Core.Sim.ActivateCombatEvent(Core.Sim.World.NextCombat());
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