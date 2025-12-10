namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class AcidHandler : BodyPartModifier
{
    public bool HasSpread;
    public bool HasPenetrated;
    public static readonly List<SubstanceType> AllowedSubstances = [
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

        var damageMultiplier = HasSpread ? .004f : 0.001f;
        var damage = BodyPart.HitPoints * damageMultiplier;
        BodyPart.HitPoints -= damage;
        if (HasPenetrated == false && BodyPart is { Type: BodyPartType.Skin, HealthPercent: < .2f })
        {
            HasPenetrated = true;
            if (BodyPart.Socket?.ParentPart?.AllInternalParts.Count != 0)
            {
                foreach (var internalPart in BodyPart.Socket!.ParentPart!.AllInternalParts)
                {
                    SpreadTo(internalPart);
                }
            }
        }

        if (HasSpread == false && BodyPart is { Type: BodyPartType.Skin, HealthPercent: < .4f })
        {
            HasSpread = true;
            if (BodyPart.Socket?.ParentPart != null)
            {
                SpreadTo(BodyPart.Socket.ParentPart);
                foreach (var externalPart in BodyPart.ExternalParts)
                {
                    SpreadTo(externalPart);
                }
            }
        }

        CheckIfLostVitalPart();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        Log.Info($"AcidHandler: Applying to part: {part.Label}");
        if(part.IsExternal == false) return false;
        Log.Info($"AcidHandler: Part is external: {part.IsExternal}");
        if (AllowedSubstances.Contains(part.Substance) == false)
        {
            Log.Info($"AcidHandler: Part substance not allowed: {part.Substance}");
            return false;
        }
        Log.Info($"AcidHandler: Part substance allowed: {part.Substance}");
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
        ScribeValues.Look(ref HasSpread, "HasSpread");
        base.ExposeData();
    }
}