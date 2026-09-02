using Wendlemire.Sim.Achievements.Handlers;

namespace Wendlemire.Sim.Achievements;

/// <summary>
/// Definition for an achievement that can be unlocked by the player
/// </summary>
[UsedImplicitly]
public class AchievementDef : Def
{
    /// <summary>Target value required to unlock (e.g., kill 10 enemies)</summary>
    public float TargetValue = 0;

    /// <summary>Benefit description for the achievement</summary>
    public string BenifitDescription = "";

    /// <summary>Marks granted when this achievement unlocks. Zero uses <see cref="AchievementRewards.MarksPerUnlock"/>.</summary>
    public int MarksReward = AchievementRewards.MarksPerUnlock;

    /// <summary>Item definition</summary>
    public ItemDef? ItemUsedDef;

    /// <summary>Item definition of the trinket that is unlocked when the achievement is unlocked</summary>
    public ItemDef? UnlockedTrinketDef;

    /// <summary>Trait definition</summary>
    public TraitDef? TraitDef;

    /// <summary>Whether this achievement is hidden until unlocked</summary>
    public bool IsHidden = false;
    
    /// <summary>Optional icon path for the achievement</summary>
    public string IconPath = "";
    
    /// <summary>Category for grouping achievements</summary>
    public string Category = "General";
    
    /// <summary>The handler class type that manages this achievement's logic</summary>
    public Type? HandlerClass;
    
    /// <summary>Runtime handler instance</summary>
    [NonSerialized]
    public AchievementHandler? Handler;
    
    public override void Initialize()
    {
        base.Initialize();
        
        // Handlers are constructed per-run by AchievementTracker via ISimFactory.
    }
}
