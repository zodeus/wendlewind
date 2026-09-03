namespace Wendlemire.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BurningHandler : BodyPartModifier
{
    public BurningHandler(IRng rng)
    {
        Rng = rng;
    }

    private bool _hasSpread;
    private bool _hasPenetrated;
    private const double BaseDamage = 0.08;
    private const double SkinDamage = 0.2;
    private const double OrganDamage = 0.003;
    private const double PenetratedDamage = 0.02;
    private const double PenetrationThreshold = 0.05;
    private const double SpreadThreshold = 0.2;
    public override List<SubstanceType> AllowedSubstances => [SubstanceType.Flesh, SubstanceType.Bone, SubstanceType.Fungus, SubstanceType.Wood, SubstanceType.Chitin];

    public override void Tick()
    {
        if (BodyPart.Modifiers.Any(m => m.Def == Defs.BodyPartModifiers.SoothingBalm))
        {
            IsExpired = true;
            return;
        }

        var damage = BaseDamage;
        if (BodyPart.IsOrgan)
        {
            damage = OrganDamage;
        }
        else if (BodyPart.Type == BodyPartType.Skin)
        {
            damage = SkinDamage;
        }
        else if (_hasPenetrated)
        {
            damage = PenetratedDamage;
        }

        BodyPart.HitPoints -= damage * Power * BurnDamageFactor();

        this.HandleSpreading(BodyPart, SpreadThresholdForTick(), ref _hasSpread);
        this.HandlePenetration(BodyPart, PenetrationThresholdForTick(), ref _hasPenetrated);
        CheckIfLostVitalPart();
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (!part.IsExternal) return false;
        if (!AllowedSubstances.Contains(part.Substance))
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


    private bool IsTallowed() =>
        BodyPart.Body?.Effects.Has(Defs.BodyEffects.Tallowed) == true;

    private double BurnDamageFactor()
    {
        var factor = 1d;
        if (IsTallowed())
        {
            factor *= ItemSynergies.TallowBurnDamage;
        }

        if (ItemSynergies.HasCausticFire(BodyPart))
        {
            factor *= ItemSynergies.CausticFireBurn;
        }

        return factor;
    }

    private double SpreadThresholdForTick()
    {
        var threshold = SpreadThreshold;
        if (IsTallowed())
        {
            threshold = Math.Max(threshold, ItemSynergies.TallowBurnSpread);
        }

        if (ItemSynergies.HasCausticFire(BodyPart))
        {
            threshold = Math.Max(threshold, ItemSynergies.CausticFireSpread);
        }

        return threshold;
    }

    private double PenetrationThresholdForTick()
    {
        var threshold = PenetrationThreshold;
        if (IsTallowed())
        {
            threshold = Math.Max(threshold, ItemSynergies.TallowBurnPenetrate);
        }

        if (ItemSynergies.HasCausticFire(BodyPart))
        {
            threshold = Math.Max(threshold, ItemSynergies.CausticFirePenetrate);
        }

        return threshold;
    }

    public override void ExposeData()
    {
        ScribeValues.Look(ref _hasSpread, "HasSpread");
        ScribeValues.Look(ref _hasPenetrated, "HasPenetrated");
        base.ExposeData();
    }

    public override InfoPanelData GetInfoData()
    {
        var currentDamage = BodyPart?.IsOrgan == true ? OrganDamage 
            : BodyPart?.Type == BodyPartType.Skin ? SkinDamage 
            : _hasPenetrated ? PenetratedDamage : BaseDamage;

        var lines = new List<InfoLine>();
        if (IsTallowed())
        {
            lines.Add(new("Clings to the tallow", InfoColors.Warning));
        }

        if (BodyPart != null && ItemSynergies.HasCausticFire(BodyPart))
        {
            lines.Add(new("Reacts with the acid", new Color(255, 180, 60)));
        }

        return new InfoPanelData
        {
            Damage = currentDamage * Power * (BodyPart == null ? 1 : BurnDamageFactor()),
            HasSpread = _hasSpread,
            HasPenetrated = _hasPenetrated,
            CuredBy = "Soothing Balm",
            ShowPower = true,
            Lines = lines
        };
    }
}