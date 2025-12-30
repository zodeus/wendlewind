namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class FesteringHandler : BodyPartModifier
{
    private const double BaseDamage = 0.1;
    private const double PenetratedDamage = 0.01;
    private const double OrganDamage = 0.005;
    private const double PenetrationThreshold = 0.75;
    private const double SpreadThreshold = 0.85;
    private bool _hasPenetrated;
    private bool _hasSpread;
    public override List<SubstanceType> AllowedSubstances => [SubstanceType.Flesh, SubstanceType.Bone, SubstanceType.Chitin];

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.IsSevered)
        {
            return;
        }
        var damage = BaseDamage;
        if (BodyPart.IsOrgan)
        {
            damage = OrganDamage;
        } else if (_hasPenetrated)
        {
            damage = PenetratedDamage;
        }

        BodyPart.HitPoints -= damage;

        this.HandleSpreading(BodyPart, SpreadThreshold, ref _hasSpread);
        this.HandlePenetration(BodyPart, PenetrationThreshold, ref _hasPenetrated);

        CheckIfLostVitalPart();
    }

    public override void MergeWith(BodyPartModifier modifier)
    {
        _hasSpread = false;
        _hasPenetrated = false;
        base.MergeWith(modifier);
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hasSpread, "HasSpread");
        ScribeValues.Look(ref _hasPenetrated, "HasPenetrated");
        base.ExposeData();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false) return false;
        if (AllowedSubstances.Contains(part.Substance) == false) return false;

        var skin = part.Skin;
        if (skin != null)
        {
            skin.TryAddModifier(this);
        }
        else
        {
            part.TryAddModifier(this);
        }

        return true;
    }
}