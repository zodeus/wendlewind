namespace Grafted.Sim.Entities.Items.Equipment;

/// <summary>
/// Handler for the Cleric Cloak - combines strength bonus with healing regeneration.
/// Delegates to StrengthCloakHandler and RejuvenationCloakHandler.
/// </summary>
public class ClericCloakHandler : EquipmentHandler, IUpgradableHandler, ICloakHandler
{
    private readonly StrengthCloakHandler _strengthHandler = new();
    private readonly RejuvenationCloakHandler _rejuvenationHandler = new();

    private int _upgradeLevel;

    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Equipment.ItemDef.UpgradeProperties;
    
    public void SetUpgradeLevel(int level)
    {
        _upgradeLevel = level;
        _strengthHandler.SetLevel(level);
        _rejuvenationHandler.SetLevel(level);
    }

    public string GetBonusDisplayText()
    {
        return $"{_strengthHandler.GetBonusDisplayText()}\n{_rejuvenationHandler.GetBonusDisplayText()}";
    }

    public override void ModifyStat(Pawn pawn, StatDef stat, ref float value)
    {
        _strengthHandler.ModifyStat(pawn, stat, ref value);
        _rejuvenationHandler.ModifyStat(pawn, stat, ref value);
    }

    public override void Tick(Pawn pawn, BodyPart bodyPart)
    {
        _strengthHandler.Tick(pawn, bodyPart);
        _rejuvenationHandler.Tick(pawn, bodyPart);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
        
        if(Scribe.State == ScribeState.PostLoadInitialization)
        {
            SetUpgradeLevel(_upgradeLevel);
        }
    }
}
