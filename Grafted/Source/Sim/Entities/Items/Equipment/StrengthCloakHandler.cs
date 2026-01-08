namespace Grafted.Sim.Entities.Items.Equipment;

public class StrengthCloakHandler : EquipmentHandler, IUpgradableHandler, ICloakHandler
{
    private const float BaseStrengthBonus = 1f;
    public const float Level1StrengthBonus = 2f;
    public const float Level2StrengthBonus = 3f;

    private int _upgradeLevel;

    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Equipment.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;

    public float StrengthBonus => _upgradeLevel switch
    {
        1 => Level1StrengthBonus,
        2 => Level2StrengthBonus,
        _ => BaseStrengthBonus
    };

    // ICloakHandler implementation
    public Color BonusColor => new(220, 180, 120);
    public string BonusLabel => "Strength";

    public string GetBonusDisplayText()
    {
        var bonus = StrengthBonus;
        return bonus > 0 
            ? $"{BonusLabel}: +{bonus:F0}" 
            : $"{BonusLabel}: No bonus";
    }
    

    public override void ModifyStat(Pawn pawn, StatDef stat, ref float value)
    {
        if (stat == Defs.Stats.Strength)
        {
            value += StrengthBonus;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
    }
}
