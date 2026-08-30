namespace Wendlewind.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class DeathRattleHandler : TrinketHandler
{
    public DeathRattleHandler(IRng rng)
    {
        Rng = rng;
    }

    public readonly RangeFloat _damage = new(80, 160);
    public const double KillDamageMultiplier = 1;
    private const int CooldownValue = 1200;

    private const int KillCooldownMultiplier = 20;

    public const int TotalHitsToCharge = 17;

    private int _maxCharges = 3;

    private int _hitsToCharge = 0;

    private int _charges;


    public int Charges => _charges;
    public int HitsToCharge => _hitsToCharge;

    public int KillCooldown => CooldownValue + (Kills * KillCooldownMultiplier);
    public int MaxCharges => _maxCharges;

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (request.TargetedPart?.Type == BodyPartType.Head)
        {
            _hitsToCharge++;
            if (_hitsToCharge >= TotalHitsToCharge)
            {
                _charges = Math.Min(_charges + 1, MaxCharges);
                _hitsToCharge = 0;
            }
        }

        if (IsActive)
        {
            return UseCharge(request);
        }

        return null;
    }

    public override void DeActivate()
    {
        base.DeActivate();
    }


    public override void OnClick()
    {
        if (_charges < 1) return;

        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }

    private DamageRecord? UseCharge(DamageRequest request)
    {
        var part = request.TargetedPart;
        if (part?.Type != BodyPartType.Head) return null;

        DeActivate();
        _charges--;
        Cooldown = KillCooldown;

        List<DamagedBodyPartRecord> damagedParts = [];
        var minDamage = _damage.Min + (float)(_damage.Min * Kills * KillDamageMultiplier);
        var maxDamage = _damage.Max + (float)(_damage.Max * Kills * KillDamageMultiplier);
        var damage = (double)new RangeFloat(minDamage, maxDamage).Roll(Context.Rng);
        part.ApplyDamageToExternalPart(new Damage(Trinket, damage, "Rattle"), damagedParts);

        if (part.Body?.Pawn.IsDeadFromPartFailure() != null)
        {
            Kills++;
        }

        var damageRecord = new DamageRecord(Trinket.Label, "Head Rattle", Trinket.ItemDef.WeaponProperties?.DamageType ?? DamageType.Invalid, part, damage, amountBlocked: 0, weaponMoniker: Trinket.ItemDef.Moniker)
        {
            ActualAmount = damage,
            BodyParts = damagedParts
        };
        damageRecord.ReflectedEffects.Add(new ReflectedStatusEffect(part.Body?.Pawn ?? null!, Trinket.ItemDef, "Death Rattle", Trinket.ItemDef.Moniker));
        return damageRecord;
    }

    

    

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hitsToCharge, "HitsToCharge");
        ScribeValues.Look(ref _charges, "Charges");
        base.ExposeData();
    }
}