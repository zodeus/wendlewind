namespace Grafted.Sim.Entities.Items.Medicinals;

public class MedicinalProperties {
    [UsedImplicitly] public Type HandlerClass = typeof(MedicinalHandler);
    public MedicinalHandler Handler => (MedicinalHandler) Activator.CreateInstance(HandlerClass)!;
}

public abstract class MedicinalHandler {
    public abstract bool ApplyToPart(BodyPart part);
}

public class BalmyOintmentHandler : MedicinalHandler {
    public override bool ApplyToPart(BodyPart part) {
        int duration = 1200;
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        foreach (BodyPart internalPart in part.AllInternalParts) {
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration));
        }

        return true;
    }
}

public class MenderMistHandler : MedicinalHandler {
    private float _mistAmount;

    public override bool ApplyToPart(BodyPart part) {
        _mistAmount = 200;
        MistPart(part);
        return _mistAmount < 200;
    }

    private void MistPart(BodyPart bodyPart) {
        if (_mistAmount <= 0) {
            return;
        }

        _mistAmount -= UpdateHealth(bodyPart);
        foreach (BodyPart internalPart in bodyPart.InternalParts) {
            if (internalPart.IsBone || internalPart.Type is BodyPartType.Skin) {
                _mistAmount -= UpdateHealth(internalPart);
            }
        }

        foreach (BodyPart externalPart in bodyPart.ExternalParts) {
            MistPart(externalPart);
        }
    }

    float UpdateHealth(BodyPart bodyPart) {
        float currentHealth = bodyPart.HitPoints;
        bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, _mistAmount);
        return bodyPart.HitPoints - currentHealth;
    }
}

public class MedKitHandler : MedicinalHandler {
    public override bool ApplyToPart(BodyPart part) {
        if (part.HealthPercent >= 1 && part.InternalParts.Any(p => p.HealthPercent < 1) == false) {
            return false;
        }

        part.HitPoints = part.MaxHitPoints;
        foreach (BodyPart internalPart in part.InternalParts) {
            internalPart.HitPoints = internalPart.MaxHitPoints;
        }

        return true;
    }
}

public class ArterialThreadsHandler : MedicinalHandler {
    public override bool ApplyToPart(BodyPart part) {
        foreach (BodyPart internalPart in part.InternalParts) {
            if (internalPart.Type == BodyPartType.Artery && internalPart.HealthPercent < 1) {
                internalPart.HitPoints = internalPart.MaxHitPoints;
                return true;
            }
        }

        return false;
    }
}