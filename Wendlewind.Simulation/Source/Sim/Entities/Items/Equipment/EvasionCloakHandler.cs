namespace Wendlewind.Sim.Entities.Items.Equipment;

public class EvasionCloakHandler : EquipmentHandler, IUpgradableHandler, ICloakHandler
{
    private const float BaseEvasionBonus = 0.05f;
    public const float Level1EvasionBonus = 0.10f;
    public const float Level2EvasionBonus = 0.15f;

    private int _upgradeLevel;

    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Equipment.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;
    
    /// <summary>
    /// Sets the upgrade level directly. Used by composite cloak handlers.
    /// </summary>
    public void SetLevel(int level) => _upgradeLevel = level;

    public float EvasionBonus => _upgradeLevel switch
    {
        1 => Level1EvasionBonus,
        2 => Level2EvasionBonus,
        _ => BaseEvasionBonus
    };

    // ICloakHandler implementation
    public Color BonusColor => new(140, 180, 220);
    public string BonusLabel => "Evasion";

    public string GetBonusDisplayText()
    {
        var bonus = EvasionBonus;
        return bonus > 0 
            ? $"{BonusLabel}: +{bonus:P0}" 
            : $"{BonusLabel}: No bonus";
    }

    public override void ModifyStat(Pawn pawn, StatDef stat, ref float value)
    {
        if (stat == Defs.Stats.Evasion)
        {
            value += EvasionBonus;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
    }
}
