namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class ElectrofiedHandler : BodyPartModifier
{
    private bool _hasSpread;
    private bool _hasPenetrated;
    private double _baseDamage = 0.3;
    private double _penetratedDamage = 0.2;
    private const double OrganDamage = 0.01;
    private const double ArteryDamage = 0.01;
    private const double EyeDamage = 0.003;
    private const double PenetrationThreshold = 0.75;
    private const double SpreadThreshold = 0.85;

    public override List<SubstanceType> AllowedSubstances =>
        [SubstanceType.Flesh, SubstanceType.Metal, SubstanceType.Bone, SubstanceType.Chitin, SubstanceType.Fungus, SubstanceType.Wood];

    public override void Tick()
    {
        var damage = _baseDamage;
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
            damage = _penetratedDamage;
        }

        damage *= Power;
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
        _baseDamage *= 1.2;
        _penetratedDamage *= 1.2;
        DurationInTicks += Core.Random.Next(0, modifier.DurationInTicks);
    }

    public override void SpreadTo(BodyPart part)
    {
        base.SpreadTo(part);
        _hasSpread = Core.Random.Chance(0.9f);
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hasSpread, "HasSpread");
        ScribeValues.Look(ref _hasPenetrated, "HasPenetrated");
        base.ExposeData();
    }

    public override Widget? GetInfoPanel()
    {
        var currentDamage = BodyPart?.IsOrgan == true ? OrganDamage 
            : BodyPart?.Type == BodyPartType.Artery ? ArteryDamage 
            : BodyPart?.Type == BodyPartType.Eye ? EyeDamage
            : _hasPenetrated ? _penetratedDamage : _baseDamage;

        return BuildInfoPanel(new InfoPanelData
        {
            Damage = currentDamage * Power,
            DamageColor = new Color(100, 180, 255),
            Lines = [new("Conducts through body", new Color(150, 200, 255))],
            HasSpread = _hasSpread,
            HasPenetrated = _hasPenetrated,
            ShowPower = true
        });
    }
}
