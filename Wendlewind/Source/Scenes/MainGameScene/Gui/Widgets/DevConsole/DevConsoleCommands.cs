using System.Text;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.DevConsole;

/// <summary>
/// Handles parsing and execution of console commands.
/// Commands start with / and follow the format: /command arg1 arg2 ...
/// </summary>
public static class DevConsoleCommands
{
    public static string Execute(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        // Commands must start with /
        if (!input.StartsWith('/'))
            return $"Commands must start with /. Type /help for available commands.";

        var parts = ParseCommand(input[1..]); // Remove leading /
        if (parts.Length == 0)
            return "Empty command.";

        var command = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        return command switch
        {
            "help" => Help(args),
            "create-entity" or "spawn" or "give" => CreateEntity(args),
            "list-items" or "items" => ListItems(args),
            "list-pawns" or "pawns" => ListPawns(args),
            "clear" => "CLEAR",
            "heal" => Heal(args),
            "kill" => Kill(args),
            "complete-zone" => CompleteZone(),
            "stats" => ShowStats(),
            "tp" or "teleport" => Teleport(args),
            "list-zones" or "zones" => ListZones(args),
            _ => $"Unknown command: /{command}. Type /help for available commands."
        };
    }

    private static string[] ParseCommand(string input)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts.ToArray();
    }

    private static string Help(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0].ToLowerInvariant() switch
            {
                "create-entity" or "spawn" or "give" =>
                    "/create-entity <moniker> [count]\n" +
                    "  Creates an item and adds it to player inventory.\n" +
                    "  Examples:\n" +
                    "    /create-entity IronSword\n" +
                    "    /create-entity HealthPotion 5\n" +
                    "    /spawn RatClaw 3",
                "list-items" or "items" =>
                    "/list-items [filter]\n" +
                    "  Lists all item monikers. Optionally filter by name.\n" +
                    "  Examples:\n" +
                    "    /list-items\n" +
                    "    /items sword",
                "heal" =>
                    "/heal [amount]\n" +
                    "  Heals the player. If no amount, fully heals.\n" +
                    "  Also restores blood and removes negative modifiers.",
                _ => $"No detailed help for '{args[0]}'"
            };
        }

        return "Available commands:\n" +
               "  /help [command]     - Show help\n" +
               "  /create-entity <moniker> [count] - Spawn item\n" +
               "  /list-items [filter] - List item monikers\n" +
               "  /list-pawns [filter] - List pawn monikers\n" +
               "  /list-zones [filter] - List zone monikers\n" +
               "  /heal [amount]      - Heal player\n" +
               "  /kill               - Kill current enemy\n" +
               "  /complete-zone      - Complete current zone\n" +
               "  /stats              - Show player stats\n" +
               "  /tp <zone-moniker>  - Teleport to zone\n" +
               "  /clear              - Clear console";
    }

    private static string CreateEntity(string[] args)
    {
        if (args.Length < 1)
            return "Usage: /create-entity <moniker> [count]\nType /list-items to see available items.";

        var moniker = args[0];
        var count = 1;

        if (args.Length >= 2 && int.TryParse(args[1], out var parsedCount))
            count = Math.Max(1, parsedCount);

        // Try to find the item definition
        var itemDef = DefRepository<ItemDef>.GetByMoniker(moniker, raiseError: false);
        if (itemDef == null)
        {
            // Try case-insensitive search
            itemDef = DefRepository<ItemDef>.Defs
                .FirstOrDefault(d => d.Moniker.Equals(moniker, StringComparison.OrdinalIgnoreCase));
        }

        if (itemDef == null)
        {
            // Suggest similar items
            var suggestions = DefRepository<ItemDef>.Defs
                .Where(d => d.Moniker.Contains(moniker, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(d => d.Moniker);

            var suggestionText = suggestions.Any()
                ? $"\nDid you mean: {string.Join(", ", suggestions)}?"
                : "\nType /list-items to see available items.";

            return $"Item not found: {moniker}{suggestionText}";
        }

        var playerPawn = Core.Context?.PlayerPawn;
        if (playerPawn == null)
            return "Error: No player pawn found.";

        // Create and add items
        var item = EntityGenerator.CreateEntity<Item>(itemDef, count);
        var added = playerPawn.Inventory.TryAdd(item);

        if (added)
            return $"Created {count}x {itemDef.Label} ({itemDef.Moniker})";
        else
            return $"Created {itemDef.Label} but couldn't add to inventory (full?).";
    }

    private static string ListItems(string[] args)
    {
        var filter = args.Length > 0 ? args[0] : null;
        var items = DefRepository<ItemDef>.Defs
            .Where(d => filter == null ||
                        d.Moniker.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        d.Label.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Moniker)
            .Take(30)
            .ToList();

        if (items.Count == 0)
            return filter != null
                ? $"No items matching '{filter}'"
                : "No items found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Items{(filter != null ? $" matching '{filter}'" : "")} ({items.Count} shown):");
        foreach (var item in items)
        {
            sb.AppendLine($"  {item.Moniker} - {item.Label}");
        }

        if (DefRepository<ItemDef>.Defs.Count > 30 && filter == null)
            sb.AppendLine($"  ... and {DefRepository<ItemDef>.Defs.Count - 30} more. Use /list-items <filter>");

        return sb.ToString().TrimEnd();
    }

    private static string ListPawns(string[] args)
    {
        var filter = args.Length > 0 ? args[0] : null;
        var pawns = DefRepository<PawnDef>.Defs
            .Where(d => filter == null ||
                        d.Moniker.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        d.Label.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Moniker)
            .Take(30)
            .ToList();

        if (pawns.Count == 0)
            return filter != null
                ? $"No pawns matching '{filter}'"
                : "No pawns found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Pawns{(filter != null ? $" matching '{filter}'" : "")} ({pawns.Count} shown):");
        foreach (var pawn in pawns)
        {
            sb.AppendLine($"  {pawn.Moniker} - {pawn.Label}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string ListZones(string[] args)
    {
        var filter = args.Length > 0 ? args[0] : null;
        var zones = DefRepository<ZoneDef>.Defs
            .Where(d => filter == null ||
                        d.Moniker.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        d.Label.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Stage)
            .ToList();

        if (zones.Count == 0)
            return filter != null
                ? $"No zones matching '{filter}'"
                : "No zones found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Zones{(filter != null ? $" matching '{filter}'" : "")}:");
        foreach (var zone in zones)
        {
            sb.AppendLine($"  {zone.Moniker} - {zone.Label} (Stage {zone.Stage})");
        }

        return sb.ToString().TrimEnd();
    }

    private static string Heal(string[] args)
    {
        var player = Core.Context?.PlayerPawn;
        if (player == null)
            return "Error: No player pawn found.";

        if (args.Length > 0 && float.TryParse(args[0], out var amount))
        {
            // Partial heal - just add to current hit points
            foreach (var part in player.Body.AllParts)
            {
                part.HitPoints = Math.Min(part.MaxHitPoints, part.HitPoints + amount);
            }
            return $"Healed player for {amount} HP on all parts.";
        }

        // Full heal - restore all hit points, blood, and clear modifiers
        foreach (var part in player.Body.AllParts)
        {
            part.HitPoints = part.MaxHitPoints;
            part.Modifiers.Clear();
        }
        player.Body.BloodAmount = player.Body.MaxBlood;

        return "Fully healed player (HP, blood, and cleared modifiers).";
    }

    private static string Kill(string[] args)
    {
        var encounter = Core.Context?.CurrentZone?.ActiveEncounter;
        if (encounter == null)
            return "No active encounter.";

        var enemy = encounter.EnemyPawns.FirstOrDefault(p => !p.IsDead);
        if (enemy == null)
            return "No living enemy found.";

        foreach (var part in enemy.Body.AllParts)
        {
            part.HitPoints = 0;
        }

        return $"Killed {enemy.LabelShort}.";
    }

    private static string CompleteZone()
    {
        var zone = Core.Context?.CurrentZone;
        if (zone == null)
            return "Not in a zone.";

        zone.IsComplete = true;
        Core.Context?.World.ProgressTracker.OnZoneCompleted(zone);
        return $"Completed zone: {zone.ZoneDef.Label}";
    }

    private static string ShowStats()
    {
        var player = Core.Context?.PlayerPawn;
        if (player == null)
            return "Error: No player pawn found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Player: {player.LabelShort}");
        sb.AppendLine($"  Blood: {player.Body.BloodAmount:F0}/{player.Body.MaxBlood:F0}");

        var stats = new[]
        {
            Defs.Stats.MaxHitPoints,
            Defs.Stats.WeaponPower,
            Defs.Stats.Strength,
            Defs.Stats.AttackSpeed,
            Defs.Stats.Accuracy,
            Defs.Stats.Evasion,
            Defs.Stats.PhysicalResistance
        };

        foreach (var stat in stats.Where(s => s != null))
        {
            sb.AppendLine($"  {stat.Label}: {player.GetStatValue(stat):F1}");
        }

        sb.AppendLine($"  Inventory: {player.Inventory.Count()} items");
        sb.AppendLine($"  Equipped weapons: {player.Equipment.UsableWeapons.Count()}");

        return sb.ToString().TrimEnd();
    }

    private static string Teleport(string[] args)
    {
        if (args.Length < 1)
            return "Usage: /tp <zone-moniker>\nType /list-zones to see available zones.";

        var context = Core.Context;
        if (context == null)
            return "Error: No game context available.";

        var moniker = args[0];
        var zoneDef = DefRepository<ZoneDef>.GetByMoniker(moniker, raiseError: false);

        if (zoneDef == null)
        {
            zoneDef = DefRepository<ZoneDef>.Defs
                .FirstOrDefault(d => d.Moniker.Contains(moniker, StringComparison.OrdinalIgnoreCase) ||
                                     d.Label.Contains(moniker, StringComparison.OrdinalIgnoreCase));
        }

        if (zoneDef == null)
        {
            var zones = DefRepository<ZoneDef>.Defs.Take(10).Select(z => z.Moniker);
            return $"Zone not found: {moniker}\nAvailable: {string.Join(", ", zones)}...";
        }

        // Check if the zone exists in the current world
        var zone = context.World.Zones.FirstOrDefault(z => z.ZoneDef == zoneDef);
        if (zone == null)
        {
            var availableZones = context.World.Zones.Take(10).Select(z => z.ZoneDef.Moniker);
            return $"Zone '{zoneDef.Label}' is not in this world.\nAvailable: {string.Join(", ", availableZones)}...";
        }
        zone.IsComplete = false;
        zone.Stage = 0;
        context.EnterZone(zoneDef);
        return $"Teleporting to {zoneDef.Label}...";
    }
}
