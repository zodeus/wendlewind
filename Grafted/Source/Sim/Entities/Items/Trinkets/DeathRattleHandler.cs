namespace Grafted.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class DeathRattleHandler : TrinketHandler
{
    private readonly RangeFloat _damage = new(50, 150);
    private const int ChargesRequired = 8;
    private const int CooldownValue = 2000;
    private const double KillDamageMultiplier = .2; // 100 percent per kills
    private const int KillCooldownMultiplier = 100;

    public override DamageRecord? HandleCombat(Pawn pawn, Pawn target)
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
        part.ApplyDamageToExternalPart(new Damage(Trinket, damage), damagedParts);

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

    private void Reset()
    {
        Cooldown = CooldownValue + (Kills * KillCooldownMultiplier);
        Charges = 0;
        IsActive = false;
    }
}