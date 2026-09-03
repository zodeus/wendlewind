namespace Wendlemire.Sim.Entities.Items.Trinkets;

[UsedImplicitly]
public class BloodyBellHandler : TrinketHandler
{
    public BloodyBellHandler(IRng rng)
    {
        Rng = rng;
    }

    public const int DefaultCooldown = 300;
    private const float BaseBloodDrainPercent = 0.08f;
    private const float BloodDrainPerRing = 0.02f;
    private const float MaxBloodDrainPercent = 0.25f;

    private int _totalRings;

    public int TotalRings => _totalRings;

    public float CurrentBloodDrainPercent => Math.Min(BaseBloodDrainPercent + (_totalRings * BloodDrainPerRing), MaxBloodDrainPercent);

    public override void OnClick()
    {
        if (Cooldown > 0)
        {
            return;
        }

        if (IsActive)
        {
            DeActivate();
        }
        else
        {
            Activate();
        }
    }

    public override DamageRecord? PostAttackHandler(Pawn victim, DamageRequest request, DamageResponse response)
    {
        if (!IsActive || Cooldown > 0)
        {
            return null;
        }

        if (victim.Body == null || request.Source.Body == null)
        {
            return null;
        }

        if (response.Damages.All(d => d.ActualAmount <= 0))
        {
            return null;
        }

        DeActivate();
        _totalRings++;
        Cooldown = DefaultCooldown;

        var drain = victim.Body.MaxBlood * CurrentBloodDrainPercent;
        victim.Body.BloodAmount -= drain;
        request.Source.Body.BloodAmount += drain;

        var part = request.TargetedPart ?? victim.Body.AllExternalParts.First();
        var record = new DamageRecord(
            Trinket.Label,
            "Blood Toll",
            DamageType.Magic,
            part,
            0,
            amountBlocked: 0,
            weaponMoniker: Trinket.ItemDef.Moniker)
        {
            ActualAmount = 0
        };
        record.ReflectedEffects.Add(new ReflectedStatusEffect(
            victim,
            Trinket.ItemDef,
            $"Bloody Bell drained {drain:N0} blood",
            Trinket.ItemDef.Moniker));
        return record;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        ScribeValues.Look(ref _totalRings, "TotalRings");
    }
}
