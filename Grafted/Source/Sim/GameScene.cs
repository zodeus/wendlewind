using System.Linq;
using Grafted.Definitions;
using Grafted.Scenes;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.CombatGuis;
using Grafted.Utils;

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
        /*Log.Info($"SIZES");
        foreach (var part in DefRepository<BodyPartDef>.Defs.OrderBy(def => def.Size)) {
            Log.Info($"{part.Label} : {part.Size}");
        }

        Log.Info("\nHIT WEIGHTS");
        foreach (var part in DefRepository<BodyPartDef>.Defs.OrderBy(def => def.HitWeight)) {
            Log.Info($"{part.Label} : {part.HitWeight}");
        }*/

        Core.Sim.World = WorldGenerator.GenerateNewWorld();

        var playerPawn = Core.Sim.World.PlayerPawns.First();

        /*foreach (BodyPart part in playerPawn.Body.AllParts) {
            Log.Info($"{part.Label} : {part.HitPoints}");
        }*/

        var combatEvent = new CombatEvent();
        //combatEvent.IsInteractive = true;
        combatEvent.AddPlayerPawn(playerPawn);
        var pawn = PawnGenerator.CreatePawn(new PawnRequest { Race = DefRepository<RaceDef>.Defs.Where(r => r.Species == Defs.Species.Skeleton).RandomElement() });
        combatEvent.AddEnemyPawn(pawn);
        var gui = new CombatGui(combatEvent);
        Core.Sim.Gui = gui;
        combatEvent.StartAsCoroutine();
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