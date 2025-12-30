namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class AcidHandler : BodyPartModifier
{
    private const double BaseDamage = 0.04f;
    private const double PenetratedDamage = 0.06f;
    private const double PenetrationThreshold = 0.4f;
    private const double SpreadThreshold = 0.2f;
    private bool _hasSpread;
    private bool _hasPenetrated;
    public override List<SubstanceType> AllowedSubstances => [
        SubstanceType.Flesh, SubstanceType.Bone, SubstanceType.Fungus,
        SubstanceType.Wood, SubstanceType.Stone, SubstanceType.Metal,
        SubstanceType.Chitin
    ];

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.Modifiers.Any(m => m.Def == Defs.BodyPartModifiers.SoothingBalm))
        {
            IsExpired = true;
            return;
        }

        var damage = _hasSpread ? PenetratedDamage : BaseDamage;
        BodyPart.HitPoints -= damage;
        
        this.HandleSpreading(BodyPart, SpreadThreshold, ref _hasSpread);
        this.HandlePenetration(BodyPart, PenetrationThreshold, ref _hasPenetrated);

        CheckIfLostVitalPart();
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

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hasSpread, "HasSpread");
        ScribeValues.Look(ref _hasPenetrated, "HasPenetrated");
        base.ExposeData();
    }
}