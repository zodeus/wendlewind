using System.Xml.Serialization;
using Grafted.Sim.Achievements.Handlers;

namespace Grafted.Sim.Achievements;

/// <summary>
/// Tracks all achievement progress for the player
/// </summary>
public class AchievementTracker : IExposable
{
    private Dictionary<string, AchievementProgress> _progress = new();

    public event Action<AchievementDef>? AchievementUnlocked;

    public IEnumerable<AchievementProgress> AllProgress => _progress.Values;
    public IEnumerable<AchievementProgress> UnlockedAchievements => _progress.Values.Where(p => p.IsUnlocked);
    public IEnumerable<AchievementProgress> LockedAchievements => _progress.Values.Where(p => !p.IsUnlocked);
    
    /// <summary>
    /// Achievements that are unlocked but not yet acknowledged/dismissed by the player.
    /// </summary>
    public IEnumerable<AchievementProgress> UnacknowledgedAchievements => 
        _progress.Values.Where(p => p.IsUnlocked && !p.IsAcknowledged);

    /// <summary>
    /// All achievement handlers for event dispatch
    /// </summary>
    private List<AchievementHandler> Handlers => DefRepository<AchievementDef>.Defs
        .Where(d => d.Handler != null)
        .Select(d => d.Handler!)
        .ToList();

    public void Initialize()
    {
        // Initialize progress for all achievement definitions
        foreach (var def in DefRepository<AchievementDef>.Defs)
        {
            if (!_progress.ContainsKey(def.Moniker))
            {
                _progress[def.Moniker] = new AchievementProgress(def);
            }
        }
    }

    public AchievementProgress? GetProgress(AchievementDef def)
    {
        return _progress.GetValueOrDefault(def.Moniker);
    }

    public bool IsUnlocked(AchievementDef def)
    {
        return _progress.TryGetValue(def.Moniker, out var progress) && progress.IsUnlocked;
    }

    public bool IsAcknowledged(AchievementDef def)
    {
        return _progress.TryGetValue(def.Moniker, out var progress) && progress.IsAcknowledged;
    }

    /// <summary>
    /// Mark an achievement as acknowledged/dismissed by the player.
    /// </summary>
    public void Acknowledge(AchievementDef def)
    {
        if (_progress.TryGetValue(def.Moniker, out var progress))
        {
            progress.IsAcknowledged = true;
        }
    }

    /// <summary>
    /// Manually unlock an achievement
    /// </summary>
    public void Unlock(AchievementDef def)
    {
        if (!_progress.TryGetValue(def.Moniker, out var progress))
        {
            progress = new AchievementProgress(def);
            _progress[def.Moniker] = progress;
        }

        if (progress.IsUnlocked) return;

        progress.IsUnlocked = true;
        progress.CurrentValue = def.TargetValue;
        progress.UnlockedAt = DateTime.Now;

        AchievementUnlocked?.Invoke(def);
    }

    // ========================================================================
    // EVENT DISPATCH METHODS - Call these from game code
    // ========================================================================

    /// <summary>
    /// Called when combat ends
    /// </summary>
    public void OnCombatEnd(AchievementCombatEndContext context)
    {
        foreach (var handler in Handlers)
        {
            handler.OnCombatEnd(context);
        }
    }

    /// <summary>
    /// Called when player takes damage
    /// </summary>
    public void OnPlayerDamaged(Pawn victim, DamageRequest request, DamageResponse response)
    {
        foreach (var handler in Handlers)
        {
            handler.OnPlayerDamaged(victim, request, response);
        }
    }

    /// <summary>
    /// Called when enemy takes damage
    /// </summary>
    public void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response)
    {
        foreach (var handler in Handlers)
        {
            handler.OnEnemyDamaged(player, enemy, request, response);
        }
    }

    /// <summary>
    /// Called when food is consumed
    /// </summary>
    public void OnItemUsed(Pawn consumer, Item item, dynamic? data = null)
    {
        foreach (var handler in Handlers)
        {
            handler.OnItemUsed(consumer, item, data);
        }
    }

    /// <summary>
    /// Called when an item is looted
    /// </summary>
    public void OnItemFound(Item item)
    {
        foreach (var handler in Handlers)
        {
            handler.OnItemFound(item);
        }
    }

    /// <summary>
    /// Called when an item is disassembled
    /// </summary>
    public void OnItemDisassembled(Item item)
    {
        foreach (var handler in Handlers)
        {
            handler.OnItemDisassembled(item);
        }
    }

    /// <summary>
    /// Called when an item is crafted
    /// </summary>
    public void OnItemCrafted(Pawn crafter, ItemDef itemDef, int amount)
    {
        foreach (var handler in Handlers)
        {
            handler.OnItemCrafted(crafter, itemDef, amount);
        }
    }

    /// <summary>
    /// Called when an enemy is killed
    /// </summary>
    public void OnEnemyKilled(Pawn enemy)
    {
        foreach (var handler in Handlers)
        {
            handler.OnEnemyKilled(enemy);
        }
    }

    public void OnBloodLost(Pawn pawn, float bloodLost)
    {
        foreach (var handler in Handlers)
        {
            handler.OnBloodLost(pawn, bloodLost);
        }
    }

    public void OnWorldRestart(GameContext context)
    {
        foreach (var handler in Handlers)
        {
            handler.RegisterTrait(context.Player.Pawn);
        }

        foreach (var handler in Handlers)
        {
            handler.RegisterTrinket(context.Player.Pawn);
        }

        foreach (var handler in Handlers)
        {
            handler.OnWorldRestart(context);
        }
    }
    public void ExposeData()
    {
        ScribeCollections.Look(ref _progress!, "AchievementProgress", LookMode.Value, LookMode.Deep);
    }
}
