namespace Grafted.Sim.Entities.Items.Equipment;

public enum RejuvenationCloakUpgradeLevel
{
    None = 0,
    Level1 = 1,
    Level2 = 2
}

public class RejuvenationCloakHandler : EquipmentHandler
{
    private const float BaseRejuvenationPerTick = 0.01f;
    public const float Level1BonusMultiplier = 2f;
    public const float Level2BonusMultiplier = 3f; 

    private RejuvenationCloakUpgradeLevel _upgradeLevel = RejuvenationCloakUpgradeLevel.None;

    public RejuvenationCloakUpgradeLevel UpgradeLevel => _upgradeLevel;

    public float RejuvenationPerTick => _upgradeLevel switch
    {
        RejuvenationCloakUpgradeLevel.Level1 => BaseRejuvenationPerTick * Level1BonusMultiplier,
        RejuvenationCloakUpgradeLevel.Level2 => BaseRejuvenationPerTick * Level2BonusMultiplier,
        _ => BaseRejuvenationPerTick
    };

    public float CurrentBonusPercent => _upgradeLevel switch
    {
        RejuvenationCloakUpgradeLevel.Level1 => (Level1BonusMultiplier - 1f) * 100f,
        RejuvenationCloakUpgradeLevel.Level2 => (Level2BonusMultiplier - 1f) * 100f,
        _ => 0f
    };

    public RejuvenationCloakUpgradeLevel? NextUpgrade => _upgradeLevel switch
    {
        RejuvenationCloakUpgradeLevel.None => RejuvenationCloakUpgradeLevel.Level1,
        RejuvenationCloakUpgradeLevel.Level1 => RejuvenationCloakUpgradeLevel.Level2,
        _ => null
    };

    public List<ResourceCount> GetUpgradeCost(RejuvenationCloakUpgradeLevel level) => level switch
    {
        RejuvenationCloakUpgradeLevel.Level1 =>
        [
            new ResourceCount(Defs.Items.ElvishLeaf, 1)
        ],
        RejuvenationCloakUpgradeLevel.Level2 =>
        [
            new ResourceCount(Defs.Items.ElvishLeaf, 1),
            new ResourceCount(Defs.Items.RhinoSkin, 1),
            new ResourceCount(Defs.Items.GoldenBean, 2)
        ],
        _ => []
    };

    public bool CanUpgrade(PawnInventory inventory)
    {
        var next = NextUpgrade;
        if (next == null) return false;

        var costs = GetUpgradeCost(next.Value);
        foreach (var cost in costs)
        {
            if (inventory.AmountOf(cost.Item) < cost.Count)
                return false;
        }

        return true;
    }

    public bool TryUpgrade(PawnInventory inventory)
    {
        var next = NextUpgrade;
        if (next == null || !CanUpgrade(inventory)) return false;

        var costs = GetUpgradeCost(next.Value);

        // Deduct resources
        List<Item> takenResources = [];
        foreach (var cost in costs)
        {
            var taken = inventory.Take(cost);
            if (taken == null)
            {
                // Rollback if something fails
                foreach (var item in takenResources)
                    inventory.TryAdd(item);
                return false;
            }
            takenResources.Add(taken);
        }

        // Destroy taken resources
        foreach (var item in takenResources)
            item.Destroy();

        _upgradeLevel = next.Value;
        return true;
    }

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
