using System.Linq;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns;

public static class PawnGenerator {
    public static Pawn CreatePawn(PawnRequest request) {
        Pawn pawn = EntityGenerator.CreateEntity<Pawn>(request.Race.Species, true);
        pawn.Race = request.Race;
        pawn.Initialize();
        RegisterTools(pawn);

        //pawn.HitPoints = pawn.MaxHitPoints;

        return pawn;
    }

    private static void RegisterTools(Pawn pawn) {
        /*foreach (ItemDef itemDef in pawn.Race.Equipment) {
            Item item = EntityGenerator.CreateEntity<Item>(itemDef);
            var returnedItems = pawn.Equipment.TryEquip(
                pawn.Body.AllParts.First(p => p.SlotFor(item)),
                item
            ).ToList();
            if (returnedItems.Any()) {
                foreach (Item returnedItem in returnedItems) {
                    Log.Error($"{returnedItem} was returned while attempting to equip on {pawn} PawnGenerator.RegisterTools");
                }
            }
        }*/
    }
}

public struct PawnRequest {
    public PawnRequest(RaceDef race) {
        Race = race;
    }

    public RaceDef Race { get; init; }
}