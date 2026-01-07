namespace Grafted.Sim.Entities.Items.Equipment;

public class RejuvenationCloakHandler : EquipmentHandler, IUpgradableHandler
{
    private const float BaseRejuvenationPerTick = 0.01f;
    public const float Level1BonusMultiplier = 2f;
    public const float Level2BonusMultiplier = 3f; 

    private int _upgradeLevel;

    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Equipment.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;

    public float RejuvenationPerTick => _upgradeLevel switch
    {
        1 => BaseRejuvenationPerTick * Level1BonusMultiplier,
        2 => BaseRejuvenationPerTick * Level2BonusMultiplier,
        _ => BaseRejuvenationPerTick
    };

    public float CurrentBonusPercent => _upgradeLevel switch
    {
        1 => (Level1BonusMultiplier - 1f) * 100f,
        2 => (Level2BonusMultiplier - 1f) * 100f,
        _ => 0f
    };

    public override void TickForPawn(Pawn pawn, BodyPart bodyPart)
    {
        base.Tick();
        var parts = pawn.Body?.AllParts ?? [];
        foreach (var part in parts)
        {
            if (part.IsSevered) { continue; }
            if (part.HitPoints <= 0) { continue; }
            if (part.HitPoints >= part.MaxHitPoints) { continue; }

            part.HitPoints += RejuvenationPerTick;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
    }
}
