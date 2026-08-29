namespace Wendlewind.Sim.Entities.Items.Trinkets;

using Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class PerforationTrapHandler : TrinketHandler, IUpgradableHandler
{
    public PerforationTrapHandler(IRng rng)
    {
        Rng = rng;
    }

    // Fuse duration ranges
    private const int DefaultMinFuse = 30;
    private const int DefaultMaxFuse = 600;
    
    // Level 2 - Timed Fuse slider range
    private const int TimedFuseMin = 5;
    private const int TimedFuseMax = 2000;
    
    // BloodDrain base duration (ticks)
    private const int BaseBloodDrainDuration = 600;
    private const int Level2DurationBonus = 900;
    private const double BloodDrainPower = 1;
    
    private int _upgradeLevel;
    private bool _isSet;
    private int _fuseTimer;
    private int _customFuseTime = 300; // Default slider value for Level 2+
    private Pawn? _currentEnemy;
    
    // UI elements
    private int _initialFuseTime;
    
    public int UpgradeLevel => _upgradeLevel;
    public bool IsSet => _isSet;
    public int FuseTimer => _fuseTimer;
    public int CustomFuseTime
    {
        get => _customFuseTime;
        set => _customFuseTime = Math.Clamp(value, TimedFuseMin, TimedFuseMax);
    }
    
    public int BloodDrainDuration => _upgradeLevel >= 2 
        ? BaseBloodDrainDuration + Level2DurationBonus 
        : BaseBloodDrainDuration;
    
    public UpgradeProperties? UpgradeProperties => Trinket.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;
    
    // Trap setting costs
    public static readonly List<ResourceCount> TrapCosts =
    [
        new() { Item = Defs.Items.Fuse, Count = 1 },
        new() { Item = Defs.Items.Fang, Count = 2 },
        new() { Item = Defs.Items.BoneShard, Count = 5 }
    ];
    
    public bool CanSetTrap()
    {
        if (_isSet) return false;
        
        // Can't set trap during combat
        if (Context.CurrentZone?.ActiveEncounter?.State == EncounterState.InProgress) return false;
        
        var inventory = Context.PlayerPawn.Inventory;
        return TrapCosts.All(cost => inventory.AmountOf(cost.Item) >= cost.Count);
    }
    
    public bool TrySetTrap()
    {
        if (!CanSetTrap()) return false;
        
        var inventory = Context.PlayerPawn.Inventory;
        
        // Take all required resources
        List<Item> takenItems = [];
        foreach (var cost in TrapCosts)
        {
            var taken = inventory.Take(cost);
            if (taken == null)
            {
                // Rollback if any resource failed
                foreach (var item in takenItems)
                    inventory.TryAdd(item);
                return false;
            }
            takenItems.Add(taken);
        }
        
        // Destroy all taken resources
        foreach (var item in takenItems)
            item.Destroy();
        
        _isSet = true;
        _fuseTimer = 0;
        _currentEnemy = null;
        
        return true;
    }
    
    public void UnsetTrap()
    {
        if (!_isSet) return;
        
        // Return resources to inventory
        var inventory = Context.PlayerPawn.Inventory;
        foreach (var cost in TrapCosts)
        {
            var item = Context.Factory.CreateEntity<Item>(cost.Item, cost.Count);
            inventory.TryAdd(item);
        }
        
        _isSet = false;
        _fuseTimer = 0;
        _currentEnemy = null;
    }
    
    public override void Tick()
    {
        base.Tick();
        
        if (!_isSet) return;
        
        // Get current combat enemy
        var combatHandler = Context.CurrentZone?.ActiveEncounter?.CombatHandler;
        if (combatHandler == null) return;
        
        // Start fuse when combat begins
        if (_fuseTimer == 0)
        {
            _currentEnemy = combatHandler.Enemy;
            
            // Determine fuse duration based on upgrade level
            if (_upgradeLevel >= 1)
            {
                // Level 1+: Use custom fuse time (Timed Fuse upgrade)
                _fuseTimer = _customFuseTime;
            }
            else
            {
                // Level 1: Random fuse time
                _fuseTimer = Context.Rng.Next(DefaultMinFuse, DefaultMaxFuse + 1);
            }
            _initialFuseTime = _fuseTimer;
        }
        
        // Count down the fuse
        _fuseTimer--;
        
        // Fuse is done - TRIGGER THE TRAP!
        if (_fuseTimer <= 0)
        {
            TriggerTrap();
        }
    }
    
    private void TriggerTrap()
    {
        if (_currentEnemy == null || _currentEnemy.IsDead)
        {
            UnsetTrap();
            return;
        }
        
        // Apply BloodDrain to all external parts of the opponent
        var externalParts = _currentEnemy.Body.AllExternalParts;
        var partsAffected = 0;
        
        foreach (var part in externalParts)
        {
            var modifier = Context.Factory.CreateModifier(Defs.BodyPartModifiers.BloodDrain, BloodDrainDuration, BloodDrainPower);
            if (modifier.ApplyToPart(part))
            {
                partsAffected++;
            }
        }
        
        // Log the effect
        if (partsAffected > 0)
        {
            Log.Info($"Perforation Trap triggered! Applied BloodDrain to {partsAffected} parts on {_currentEnemy.LabelShort}");
        }
        
        // Reset the trap (consumed)
        _isSet = false;
        _fuseTimer = 0;
        _currentEnemy = null;
    }
    
    public override void Stop()
    {
        // Reset enemy reference when combat ends, but keep the trap set
        _currentEnemy = null;
        _fuseTimer = 0;
        base.Stop();
    }
    
    public override void OnClick()
    {
        // Clicking toggles between set/unset if possible
        if (_isSet)
        {
            // Can't unset during combat
            if (Context.CurrentZone?.ActiveEncounter?.CombatHandler != null)
            {
                return;
            }
            UnsetTrap();
        }
        else
        {
            TrySetTrap();
        }
    }
    
    
    
    
    
    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
        ScribeValues.Look(ref _isSet, "IsSet");
        ScribeValues.Look(ref _fuseTimer, "FuseTimer");
        ScribeValues.Look(ref _customFuseTime, "CustomFuseTime", 300);
        ScribeValues.Look(ref _initialFuseTime, "InitialFuseTime");
    }
}
