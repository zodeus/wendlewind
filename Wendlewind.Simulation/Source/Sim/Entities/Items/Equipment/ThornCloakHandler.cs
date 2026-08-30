using Wendlewind.Sim.Achievements.Handlers;

namespace Wendlewind.Sim.Entities.Items.Equipment;

/// <summary>
/// Handler for the Cloak of Thorns - reflects a portion of damage taken back to attackers.
/// </summary>
public class ThornCloakHandler : EquipmentHandler, IUpgradableHandler, ICloakHandler
{
    public ThornCloakHandler(IRng rng)
    {
        Rng = rng;
    }

    private const float BaseReflectPercent = 0.40f;
    public const float Level1ReflectPercent = 0.60f;
    public const float Level2ReflectPercent = 0.80f;

    private int _upgradeLevel;

    public int UpgradeLevel => _upgradeLevel;
    public UpgradeProperties? UpgradeProperties => Equipment.ItemDef.UpgradeProperties;
    void IUpgradableHandler.SetUpgradeLevel(int level) => _upgradeLevel = level;

    /// <summary>
    /// Sets the upgrade level directly. Used by composite cloak handlers.
    /// </summary>
    public void SetLevel(int level) => _upgradeLevel = level;

    public float ReflectPercent => _upgradeLevel switch
    {
        1 => Level1ReflectPercent,
        2 => Level2ReflectPercent,
        _ => BaseReflectPercent
    };

    public string GetBonusDisplayText()
    {
        return $"Reflect: {ReflectPercent:P0} of damage";
    }

    public override void PostPawnDamageTakenEffect(BodyPart bodyPart, Pawn pawn, Pawn target, DamageRecord damageRecord)
    {
        // Reflect damage back to the attacker
        if (target.Body?.AllParts == null) return;

        var reflectedDamage = damageRecord.TotalDamage * ReflectPercent;
        if (reflectedDamage <= 0) return;

        // Find a random external part on the attacker to damage
        var targetPart = target.Body.AllExternalParts
            .Where(p => !p.IsDestroyed && p.HitPoints > 0 && p.Type != BodyPartType.Eye)
            .RandomElementByWeight(p => p.HitWeight, Context.Rng);

        if (targetPart == null) return;

        // Apply thorn damage directly to the attacker's body part
        targetPart.HitPoints -= reflectedDamage;
        damageRecord.ReflectedEffects.Add(
            new ReflectedStatusEffect(target, Equipment.ItemDef, $"/c[{TC.BodyPart}]{targetPart.Label} /c[{TC.Default}]was hit by /c[{TC.BrightBlue}]{Equipment.Label} for {reflectedDamage:N0} damage", Equipment.ItemDef.Moniker)
        );
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _upgradeLevel, "UpgradeLevel");
    }
}
