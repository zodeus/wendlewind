namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class DeathRattleHandler : TrinketHandler
{
    private readonly RangeFloat _damage = new(100, 200);
    private const int ChargesRequired = 10;
    private const int CooldownValue = 1200;
    private const double KillDamageMultiplier = .2; // 100 percent per kills
    private const int KillCooldownMultiplier = 100;

    public override DamageRecord? HandleAttack(DamageRequest request, Pawn target)
    {
        Charges++;
        if (Charges < ChargesRequired) return null;

        var part = target.Body.AllExternalParts.FirstOrNull(p => p?.Type == BodyPartType.Head);
        if (part == null)
        {
            return null;
        }

        Reset();

        List<DamagedBodyPartRecord> damagedParts = [];
        var minDamage = _damage.Min + (float)(_damage.Min * Kills * KillDamageMultiplier);
        var maxDamage = _damage.Max + (float)(_damage.Max * Kills * KillDamageMultiplier);
        var damage = (double)new RangeFloat(minDamage, maxDamage).RandomValue;
        part.ApplyDamageToExternalPart(new Damage(Trinket, damage, "Rattle"), damagedParts);

        if (part.DidPawnDieFromPartFailure())
        {
            Kills++;
        }

        return new DamageRecord(Trinket.Label, "Head Rattle", Trinket.ItemDef.WeaponProperties?.DamageType ?? DamageType.Invalid, part, damage)
        {
            ActualAmount = damage,
            BodyParts = damagedParts
        };
    }

    public override void DeActivate()
    {
        Charges = 0;
        base.DeActivate();
    }

    private void Reset()
    {
        Cooldown = CooldownValue + (Kills * KillCooldownMultiplier);
        Charges = 0;
        IsActive = false;
    }
}