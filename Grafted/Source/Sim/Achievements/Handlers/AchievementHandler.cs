namespace Grafted.Sim.Achievements.Handlers;

/// <summary>
/// Base class for achievement handlers that check conditions and unlock achievements
/// </summary>
public abstract class AchievementHandler
{
    public AchievementDef Def { get; set; } = null!;

    protected AchievementTracker Tracker => Core.Context.Achievements;

    /// <summary>
    /// Check if this achievement is already unlocked
    /// </summary>
    protected bool IsUnlocked => Tracker.IsUnlocked(Def);

    /// <summary>
    /// Unlock this achievement
    /// </summary>
    protected void Unlock() => Tracker.Unlock(Def);

    /// <summary>
    /// Get or update progress for this achievement
    /// </summary>
    protected AchievementProgress Progress => Tracker.GetProgress(Def)!;

    /// <summary>
    /// Called when combat ends (win or lose)
    /// </summary>
    public virtual void OnCombatEnd(AchievementCombatEndContext context) { }

    /// <summary>
    /// Called when the player takes damage
    /// </summary>
    public virtual void OnPlayerDamaged(Pawn victim, DamageRequest request, DamageResponse response) { }

    /// <summary>
    /// Called when the player deals damage to an enemy
    /// </summary>
    public virtual void OnEnemyDamaged(Pawn player, Pawn enemy, DamageRequest request, DamageResponse response) { }

    public virtual void OnItemUsed(Pawn consumer, Item item)
    {
        if (IsUnlocked) return;
        
        // Check if this item matches the achievement's target item
        if (Def.ItemUsedDef == null || item.Def != Def.ItemUsedDef) return;
        
        Progress.CurrentValue++;
        if (Progress.CurrentValue >= Def.TargetValue)
        {
            Unlock();
        }
    }

    /// <summary>
    /// Called when an item is found
    /// </summary>
    public virtual void OnItemFound(Item item) { }

    /// <summary>
    /// Called when an item is disassembled
    /// </summary>
    public virtual void OnItemDisassembled(Item item) { }

    /// <summary>
    /// Called when an enemy is killed
    /// </summary>
    public virtual void OnEnemyKilled(Pawn enemy) { }

    /// <summary>
    /// Called when blood is lost
    /// </summary>
    public virtual void OnBloodLost(Pawn pawn, float bloodLost) { }

    /// <summary>
    /// Called when the world is restarted
    /// </summary>
    public virtual void OnWorldRestart(GameContext context) { }

    /// <summary>
    /// Called when the world is restarted
    /// </summary>
    public virtual void RegisterTrait(Pawn pawn)
    {
        if (IsUnlocked == false) return;
        if (Def.TraitDef == null) return;

        pawn.Traits.Add(Def.TraitDef);
    }

    public virtual void RegisterTrinket(Pawn pawn)
    {
        if (IsUnlocked == false) return;
        if (Def.UnlockedTrinketDef == null) return;

        pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Def.UnlockedTrinketDef));
    }
}