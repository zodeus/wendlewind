namespace Grafted.Sim.Combat;

public class DamageRecord
{
    public readonly string WeaponLabel;
    public readonly string WeaponManeuverLabel;
    public readonly DamageType DamageType;
    public readonly BodyPart BodyPartHit;
    public readonly List<ReflectedStatusEffect> ReflectedEffects = [];
    public IReadOnlyList<DamagedBodyPartRecord> BodyParts = new List<DamagedBodyPartRecord>();
    public readonly List<DestroyedItemRecord> DestroyedEquipment = [];
    public readonly double TotalDamage;
    public readonly double AmountBlocked;
    public double ActualAmount;
    public readonly bool IsCritical;

    public DamageRecord(string weaponLabel, string weaponManeuverLabel, DamageType damageType, BodyPart bodyPartHit, double totalDamage, double amountBlocked, bool isCritical = false)
    {
        WeaponLabel = weaponLabel;
        WeaponManeuverLabel = weaponManeuverLabel;
        DamageType = damageType;
        BodyPartHit = bodyPartHit;
        TotalDamage = totalDamage;
        AmountBlocked = amountBlocked;
        IsCritical = isCritical;
    }
}