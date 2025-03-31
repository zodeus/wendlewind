namespace Grafted.Sim.Combat;

public class DamageRecord
{
    public readonly string WeaponLabel;
    public readonly string WeaponManeuverLabel;
    public readonly DamageType DamageType;
    public readonly BodyPart BodyPartHit;
    public readonly List<AfflictionRecord> SourceAfflictions = [];
    public IReadOnlyList<DamagedBodyPartRecord> BodyParts = new List<DamagedBodyPartRecord>();
    public readonly List<DestroyedItemRecord> DestroyedEquipment = [];
    public readonly double TotalDamage;
    public double ActualAmount;

    public DamageRecord(string weaponLabel, string weaponManeuverLabel, DamageType damageType, BodyPart bodyPartHit, double totalDamage)
    {
        WeaponLabel = weaponLabel;
        WeaponManeuverLabel = weaponManeuverLabel;
        DamageType = damageType;
        BodyPartHit = bodyPartHit;
        TotalDamage = totalDamage;
    }

    public double AmountBlocked => TotalDamage - ActualAmount;
}