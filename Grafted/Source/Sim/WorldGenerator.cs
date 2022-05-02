using Grafted.Definitions;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim;

public static class WorldGenerator {
    public static World GenerateNewWorld(PawnConfigDef startingPawn) {
        World world = new();
        world.Initialize();
        world.AddPlayerPawn(GeneratePlayerPawn(startingPawn));
        return world;
    }

    public static Pawn GeneratePlayerPawn(PawnConfigDef startingPawn) {
        return PawnGenerator.CreatePawn(new PawnRequest(DefRepository<RaceDef>.GetByMoniker("Caucasian")!, startingPawn));
    }
}