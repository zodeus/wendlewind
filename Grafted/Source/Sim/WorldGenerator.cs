using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Utils;

namespace Grafted.Sim;

public static class WorldGenerator {
    public static World GenerateNewWorld() {
        World world = new();
        world.Initialize();
        world.AddPlayerPawn(GeneratePlayerPawn());
        return world;
    }

    private static Pawn GeneratePlayerPawn() {
        Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest { Race = DefRepository<RaceDef>.GetByMoniker("Caucasian")! });

        //Equipment
        Item knife = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker(new List<string> { "Knife" }.RandomElement())!);
        BodyPart hand1 = pawn.Body.AllParts.Where(p => p.SlotFor(knife) != null).ToList()[0];
        pawn.Equipment.TryEquip(hand1, knife);

        // Items
        Item medKit = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("MedKit")!);
        medKit.StackSize = 3;
        pawn.Inventory.Items.TryAdd(medKit);
        
        Item sutures = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("ArterialThreads")!);
        sutures.StackSize = 7;
        pawn.Inventory.Items.TryAdd(sutures);

        Item cauterize = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("Cauterize")!);
        cauterize.StackSize = 99;
        pawn.Inventory.Items.TryAdd(cauterize);
        return pawn;
    }
}