using Grafted.Definitions;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim;

public static class WorldGenerator {
    public static World GenerateNewWorld() {
        World world = new();
        world.Initialize();
        world.AddPlayerPawn(GeneratePlayerPawn());
        return world;
    }

    private static Pawn GeneratePlayerPawn() {
        Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest(
            DefRepository<RaceDef>.GetByMoniker("Caucasian")!,
            Defs.PawnConfigs.PlayerPawn
        ));
        
        return pawn;
    }
}