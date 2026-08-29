namespace Wendlewind.Sim;

public static class WorldGenerator
{
    public static World GenerateNewWorld(GameContext context)
    {
        Player player = new() { Context = context };
        player.Initialize();

        World world = new() { Context = context };
        world.Initialize(player, DefRepository<ZoneDef>.Defs);
        return world;
    }
}