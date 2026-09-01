﻿namespace Wendlemire.Sim;

public static class WorldGenerator
{
    public static World GenerateNewWorld(GameContext context, string? playerName = null)
    {
        Player player = new() { Context = context };
        player.Initialize(playerName);

        World world = new() { Context = context };
        world.Initialize(player, DefRepository<ZoneDef>.Defs);
        return world;
    }
}