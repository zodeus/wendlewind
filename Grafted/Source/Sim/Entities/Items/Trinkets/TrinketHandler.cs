namespace Grafted.Sim.Entities.Items.Trinkets;

public abstract class TrinketHandler : IExposable
{
    public bool IsActive { get; protected set; }
    public int Cooldown;
    public int Charges;
    public int Kills;

    public Item Trinket = null!;

    public string Label => Trinket.Label;

    public virtual void Tick()
    {
        Cooldown = Math.Clamp(Cooldown - 1, 0, int.MaxValue);
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Trinket!, "Def");
        ScribeValues.Look(ref Cooldown, "Cooldown");
        ScribeValues.Look(ref Charges, "Charges");
        ScribeValues.Look(ref Kills, "Kills");
    }

    public override string ToString()
    {
        return $"{Trinket.Label} Handler";
    }

    public virtual DamageRecord? HandleCombat(Pawn pawn, Pawn target)
    {
        return null;
    }

    public virtual bool Activate()
    {
        if (Cooldown > 0)
        {
            return false;
        }

        IsActive = true;

        return true;
    }

    public virtual void DeActivate()
    {
        Charges = 0;
        IsActive = false;
    }

    public virtual void Stop()
    {
        DeActivate();
    }
}

[UsedImplicitly]
public class DeathRattleHandler : TrinketHandler
{
    private readonly RangeFloat _damage = new(30, 90);
    private const int ChargesRequired = 8;
    private const int CooldownValue = 1000;
    private const double KillDamageMultiplier = .2; // 100 percent per kills
    private const int KillCooldownMultiplier = 200;

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