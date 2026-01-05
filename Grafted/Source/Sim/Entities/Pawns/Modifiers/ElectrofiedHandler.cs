namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class ElectrofiedHandler : BodyPartModifier
{
    private bool _hasSpread;
    private bool _hasPenetrated;
    private const double BaseDamage = 0.3;
    private const double PenetratedDamage = 0.005;
    private const double OrganDamage = 0.01;
    private const double ArteryDamage = 0.005;
    private const double EyeDamage = 0.003;
    private const double PenetrationThreshold = 0.75;
    private const double SpreadThreshold = 0.8;

    public override List<SubstanceType> AllowedSubstances =>
        [SubstanceType.Flesh, SubstanceType.Metal, SubstanceType.Bone, SubstanceType.Chitin, SubstanceType.Fungus, SubstanceType.Wood];

    public override void Tick()
    {
        var damage = BaseDamage;
        if (BodyPart.IsOrgan)
        {
            damage = OrganDamage;
        }
        else if (BodyPart.Type == BodyPartType.Artery)
        {
            damage = ArteryDamage;
        }
        else if (BodyPart.Type == BodyPartType.Eye)
        {
            damage = EyeDamage;
        }
        else if (_hasPenetrated)
        {
            damage = PenetratedDamage;
        }

        BodyPart.HitPoints -= damage;

        this.HandleSpreading(BodyPart, SpreadThreshold, ref _hasSpread);
        this.HandlePenetration(BodyPart, PenetrationThreshold, ref _hasPenetrated);
        CheckIfLostVitalPart();
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false) return false;
        if (AllowedSubstances.Contains(part.Substance) == false)
        {
            return false;
        }

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

    public override void MergeWith(BodyPartModifier modifier)
    {
        _hasSpread = false;
        _hasPenetrated = false;
        base.MergeWith(modifier);
    }

    public override void SpreadTo(BodyPart part)
    {
        base.SpreadTo(part);
        _hasSpread = Core.Random.Chance(0.92f);
        if (_hasSpread == false)
        {
            Log.Info($"ElectrofiedHandler: SUPER SPREAD: Spreading to {part.Label}");
        }
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hasSpread, "HasSpread");
        ScribeValues.Look(ref _hasPenetrated, "HasPenetrated");
        base.ExposeData();
    }
}
