
namespace Wendlewind.Sim.Entities.Items.Weapons;

/// <summary>
/// Handler for the BloodSuckler unique weapon.
/// On hit: if the opponent's blood type matches the attacker's, restores some blood.
/// If blood is already full, restores a small random amount of health to a random number of damaged parts.
/// </summary>
[UsedImplicitly]
public class BloodSucklerHandler : WeaponHandler
{
    private const float BloodRestorePercent = 0.03f; // 3% of max blood
    private const float HealthRestoreMin = 5f;
    private const float HealthRestoreMax = 30f;
    private const int MinPartsToHeal = 3;
    private const int MaxPartsToHeal = 6;
    
    // Tracking stats
    private float _totalBloodDrained;
    private float _totalHealthRestored;
    private int _successfulDrains;

    public override void OnHit(Pawn attacker, Pawn victim, DamageRequest request, DamageRecord damageRecord)
    {
        // Only trigger on successful hits that deal damage
        if (damageRecord.ActualAmount <= 0)
        {
            return;
        }

        // Check if blood types match
        var attackerBloodType = attacker.Body.Def.BloodType;
        var victimBloodType = victim.Body.Def.BloodType;

        if (attackerBloodType == null || victimBloodType == null)
        {
            return;
        }

        if (attackerBloodType != victimBloodType)
        {
            return;
        }

        // Blood types match - check if we need blood or health
        _successfulDrains++;
        victim.Body.BloodAmount -= attacker.Body.MaxBlood * BloodRestorePercent;
        if (attacker.Body.BloodPercent < 0.92f)
        {
            // Restore blood
            var bloodToRestore = attacker.Body.MaxBlood * BloodRestorePercent;
            attacker.Body.BloodAmount += bloodToRestore;
            _totalBloodDrained += bloodToRestore;

            damageRecord.ReflectedEffects.Add(new ReflectedStatusEffect(
                attacker,
                Weapon.ItemDef,
                $"Blood Suckler drained {bloodToRestore:N0} blood"));
        }
        else
        {
            // Blood is full - heal random damaged parts
            HealRandomDamagedParts(attacker, damageRecord);
        }
    }

    private void HealRandomDamagedParts(Pawn pawn, DamageRecord damageRecord)
    {
        // Find all damaged parts (health < max)
        var damagedParts = pawn.Body.AllParts
            .Where(p => p.HealthPercent < 0.99)
            .ToList();

        if (damagedParts.Count == 0)
        {
            return;
        }

        // Determine how many parts to heal
        var partsToHeal = GameContext.Random.Next(MinPartsToHeal, Math.Min(MaxPartsToHeal + 1, damagedParts.Count + 1));

        // Shuffle and take random parts
        var partsToHealList = damagedParts
            .InRandomOrder()
            .Take(partsToHeal)
            .ToList();

        foreach (var part in partsToHealList)
        {
            var healAmount = GameContext.Random.NextFloat(HealthRestoreMin, HealthRestoreMax);
            var prevHealth = part.HitPoints;
            part.HitPoints = Math.Min(part.MaxHitPoints, part.HitPoints + healAmount);
            var actualHeal = part.HitPoints - prevHealth;

            if (actualHeal > 0)
            {
                _totalHealthRestored += (float)actualHeal;
                damageRecord.ReflectedEffects.Add(new ReflectedStatusEffect(
                    pawn,
                    Weapon.ItemDef,
                    $"Blood Suckler healed {part.Label} for {actualHeal:N1}"));
            }
        }
    }

    
    
    

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _totalBloodDrained, "TotalBloodDrained");
        ScribeValues.Look(ref _totalHealthRestored, "TotalHealthRestored");
        ScribeValues.Look(ref _successfulDrains, "SuccessfulDrains");
    }
}
