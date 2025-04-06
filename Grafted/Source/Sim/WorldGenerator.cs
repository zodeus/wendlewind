namespace Grafted.Sim;

public static class WorldGenerator
{
    public static World GenerateNewWorld()
    {
        Player player = new();
        player.Initialize(GeneratePlayerPawn(
            "Nameless",
            DefRepository<PawnDef>.GetByMoniker("Journeyman")!,
            Defs.PawnLoadouts.DefaultStarterLoadout
        ));

        World world = new();
        world.Initialize(player, DefRepository<BiomeDef>.Defs);
        return world;
    }

    private static Pawn GeneratePlayerPawn(string name, PawnDef pawn, PawnLoadoutDef loadout)
    {
        return PawnGenerator.CreatePawn(new PawnRequest(name, pawn, loadout, PawnType.Player));
    }
}