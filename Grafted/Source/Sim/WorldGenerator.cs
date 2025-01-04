using Grafted.Sim.Entities;

namespace Grafted.Sim;

public static class WorldGenerator
{
    public static World GenerateNewWorld(PawnConfigDef startingPawn)
    {
        Player player = new();
        player.Initialize(GeneratePlayerPawn(DefRepository<RaceDef>.GetByMoniker("Journeyman")!, startingPawn));
        player.Pawn.Inventory.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.AcidFlask));
        player.Pawn.Inventory.Entities.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.PumpinJuice));
        World world = new();
        world.Initialize(player, DefRepository<BiomeDef>.Defs);
        return world;
    }

    public static Pawn GeneratePlayerPawn(RaceDef race, PawnConfigDef startingPawn)
    {
        return PawnGenerator.CreatePawn(new PawnRequest(race, startingPawn));
    }
}