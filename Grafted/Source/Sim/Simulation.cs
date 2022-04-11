using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.UI;
using Grafted.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Grafted.Sim;

public class Simulation {
    public SimulationMessages Messages = new();
    public IdProvider IdProvider = new();
    public World World = null!;
    public SimulationGui? Gui { get; set; }
    public bool IsPaused => Core.PauseCoroutines;

    public float GameSpeed = .2f;

    public void Update(float deltaTime) {
        HandleInput();
        Gui!.HandleInput();
        Gui.Update(deltaTime);
    }

    public void FixedUpdate() { }

    private void HandleInput() {
        if (Input.IsKeyPressed(Keys.Space)) {
            Core.Sim.TogglePause();
        }
    }

    public void TogglePause() {
        Core.PauseCoroutines = !Core.PauseCoroutines;
    }

    public void Draw(SpriteBatch spriteBatch, float deltaTime) {
        Gui.Render(spriteBatch);
    }

    public void Save(string filePath) {
        Log.Info("Saving Game to " + filePath);
    }

    public void Load(string filePath) {
        Log.Info("Loading from " + filePath);
    }

    public void GenerateNewWorld() {
        World = new World();
        World.Initialize();
        Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest { Race = DefRepository<RaceDef>.GetByMoniker("Caucasian")! });
        var knife = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker(new List<string> { "Knife" }.RandomElement())!);
        var hand1 = pawn.Body.AllParts.Where(p => p.SlotFor(knife) != null).ToList()[0];
        var hand2 = pawn.Body.AllParts.Where(p => p.SlotFor(knife) != null).ToList()[1];
        pawn.Equipment.TryEquip(hand1, knife);
        pawn.Equipment.TryEquip(hand2, EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!));

        var footWeapon1 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
        var foot1 = pawn.Body.AllParts.Where(p => p.SlotFor(footWeapon1) != null).ToList()[0];
        pawn.Equipment.TryEquip(foot1, footWeapon1);

        var footWeapon2 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
        var foot2 = pawn.Body.AllParts.Where(p => p.SlotFor(footWeapon2) != null).ToList()[1];
        pawn.Equipment.TryEquip(foot2, footWeapon2);

        var medkit = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("MedKit")!);
        medkit.StackSize = 3;
        pawn.Inventory.Items.TryAdd(medkit);
        var cauterize = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("Cauterize")!);
        cauterize.StackSize = 5;
        pawn.Inventory.Items.TryAdd(cauterize);
        var sutures = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("ArterialThreads")!);
        sutures.StackSize = 10;
        pawn.Inventory.Items.TryAdd(sutures);

        World.AddPlayerPawn(pawn);
    }
}