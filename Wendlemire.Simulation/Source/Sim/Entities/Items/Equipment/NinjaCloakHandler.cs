namespace Wendlemire.Sim.Entities.Items.Equipment;

/// <summary>
/// Handler for the Ninja Cloak - combines strength bonus with evasion bonus.
/// Delegates strength to StrengthCloakHandler, handles evasion directly (EvasionCloak uses BaseStats).
/// </summary>
public class NinjaCloakHandler : EquipmentHandler, IUpgradableHandler, ICloakHandler
{
    private readonly StrengthCloakHandler _strengthHandler;

    public NinjaCloakHandler(IRng rng)
    {
        Rng = rng;
        _strengthHandler = new StrengthCloakHandler(rng);
    }
    
    // Evasion bonuses (EvasionCloak doesn't have a handler, it uses BaseStats)
    private const float BaseEvasionBonus = 0.1f;
    private const float Level1EvasionBonus = 0.15f;
    private const float Level2EvasionBonus = 0.2f;

    private int _upgradeLevel;

    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Equipment.ItemDef.UpgradeProperties;
    
    void IUpgradableHandler.SetUpgradeLevel(int level)
    {
        _upgradeLevel = level;
        _strengthHandler.SetLevel(level);
    }

    private float EvasionBonus => _upgradeLevel switch
    {
        1 => Level1EvasionBonus,
        2 => Level2EvasionBonus,
        _ => BaseEvasionBonus
    };

    // ICloakHandler implementation
    public Color BonusColor => new(80, 80, 100);
    public string BonusLabel => "Shadow Power";

    public string GetBonusDisplayText()
    {
        return $"{_strengthHandler.GetBonusDisplayText()}\nEvasion: +{EvasionBonus:P0}";
    }

    public override void ModifyStat(Pawn pawn, StatDef stat, ref float value)
    {
        _strengthHandler.ModifyStat(pawn, stat, ref value);
        
        if (stat == Defs.Stats.Evasion)
        {
            value += EvasionBonus;
        }
    }

    public override void Tick(Pawn pawn, BodyPart bodyPart)
    {
        _strengthHandler.Tick(pawn, bodyPart);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
        
        // Sync component handler on load
        _strengthHandler.SetLevel(_upgradeLevel);
    }
}
