namespace Grafted.Sim;

public static class WorldGenerator
{
    public static World GenerateNewWorld()
    {
        Player player = new();
        player.Initialize();

        

        World world = new();
        world.Initialize(player, DefRepository<BiomeDef>.Defs);
        return world;
    }
}