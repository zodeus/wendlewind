namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class BurningHandler : BodyPartModifier
{
    public bool HasSpread;
    public bool HasPenetrated;

    private const double BaseDamage = 0.008;
    private const double PenetratedDamage = 0.02;
    private const double PenetrationThreshold = 0.1;
    private const double SpreadThreshold = 0.3;

    public override void Tick()
    {
        base.Tick();
        if (BodyPart.Modifiers.Any(m => m.Def == Defs.BodyPartModifiers.SoothingBalm))
        {
            IsExpired = true;
            return;
        }

        var damageMultiplier = HasPenetrated ? PenetratedDamage : BaseDamage;
        var damage = BodyPart.HitPoints * damageMultiplier;
        BodyPart.HitPoints -= damage;
        if (HasPenetrated == false && BodyPart is { Type: BodyPartType.Skin, HealthPercent: < PenetrationThreshold })
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

        if (HasSpread == false && BodyPart is { Type: BodyPartType.Skin, HealthPercent: < SpreadThreshold })
        {
            HasSpread = true;
            if (BodyPart.Socket?.ParentPart != null)
            {
                SpreadTo(BodyPart.Socket.ParentPart);
                if (BodyPart.Socket.ParentPart.Socket?.ParentPart is { } part)
                {
                    SpreadTo(part);
                }

                foreach (var externalPart in BodyPart.Socket.ParentPart.ExternalParts)
                {
                    SpreadTo(externalPart);
                }
            }
        }

        CheckIfLostVitalPart(BodyPart);
    }

    private bool CheckIfLostVitalPart(BodyPart bodyPart)
    {
        if (bodyPart.IsFunctional) return false;
        foreach (var internalPart in bodyPart.InternalParts.InRandomOrder())
        {
            if (!internalPart.IsVital) continue;
            if (CheckIfLostVitalPart(internalPart))
            {
                return true;
            }
        }

        var remainingFunctionalParts = bodyPart.Body!.AllParts.Count(p => p.Type == bodyPart.Type && p.IsFunctional);
        if (bodyPart is { IsVital: true, IsFunctional: false } && remainingFunctionalParts <= 0)
        {
            bodyPart.Body.Pawn.TriggerDeath($"{bodyPart.Label} {(bodyPart.IsDestroyed ? "was destroyed" : "stopped functioning")}");
            return true;
        }

        return false;
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.Type is not BodyPartType.Skin) return false;

        part.TryAddModifier(this);
        return true;
    }

    public override void MergeWith(BodyPartModifier modifier)
    {
        HasSpread = false;
        HasPenetrated = false;
        base.MergeWith(modifier);
    }


    public override void ExposeData()
    {
        ScribeValues.Look(ref HasSpread, "HasSpread");
        base.ExposeData();
    }
}