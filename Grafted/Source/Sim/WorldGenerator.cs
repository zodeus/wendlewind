using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim;

public static class WorldGenerator {
    public static World GenerateNewWorld(PawnConfigDef startingPawn) {
        Player player = new();
        player.Initialize(GeneratePlayerPawn(DefRepository<RaceDef>.GetByMoniker("Journeyman")!, startingPawn));
        player.Pawn.Inventory.Entities.TryAdd(EntityGenerator.CreateEntity<EssenceShard>(Defs.Items.EssenceShard));
        World world = new();
        world.Initialize(player);
        world.Time.CurrentTimeInSeconds = SimTime.HoursToSeconds(8);
        return world;
    }

    public static Pawn GeneratePlayerPawn(RaceDef race, PawnConfigDef startingPawn) {
        return PawnGenerator.CreatePawn(new PawnRequest(race, startingPawn));
    }
}