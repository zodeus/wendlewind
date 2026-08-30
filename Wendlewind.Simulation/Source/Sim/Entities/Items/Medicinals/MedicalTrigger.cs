namespace Wendlewind.Sim.Entities.Items.Medicinals;

public class MedicalTrigger : IExposable
{
    public MedicalTriggerType Type = MedicalTriggerType.Immediately;
    public MedicalTargetSelector TargetSelector = MedicalTargetSelector.Auto;
    public float Threshold;
    public float AfterSeconds;
    public float HealthThreshold = 0.6f;
    public string? TargetPartKey;

    public MedicalTrigger Clone()
    {
        return new MedicalTrigger
        {
            Type = Type,
            TargetSelector = TargetSelector,
            Threshold = Threshold,
            AfterSeconds = AfterSeconds,
            HealthThreshold = HealthThreshold,
            TargetPartKey = TargetPartKey
        };
    }

    public bool ShouldFire(Pawn self, Pawn enemy, int tick)
    {
        return Type switch
        {
            MedicalTriggerType.Immediately => true,
            MedicalTriggerType.AfterSeconds => tick >= AfterSeconds * GameContext.TicksPerSecond,
            MedicalTriggerType.SelfBloodBelow => self.Body.BloodPercent < Threshold,
            MedicalTriggerType.SelfPartsDamaged => IsSelfPartsDamaged(self),
            MedicalTriggerType.PartBelowHealth => HasPartBelowHealth(self),
            MedicalTriggerType.PartSevered => FindUnsealedSocket(self) != null,
            _ => false
        };
    }

    public string Describe()
    {
        var when = Type switch
        {
            MedicalTriggerType.Immediately => "Use immediately",
            MedicalTriggerType.AfterSeconds => $"Use after {AfterSeconds:0.##}s",
            MedicalTriggerType.SelfBloodBelow => $"Use when own blood < {Threshold * 100:0}%",
            MedicalTriggerType.SelfPartsDamaged =>
                $"Use when {Threshold * 100:0}% of parts are below {HealthThreshold * 100:0}% health",
            MedicalTriggerType.PartBelowHealth => $"Use when a part is below {HealthThreshold * 100:0}% health",
            MedicalTriggerType.PartSevered => "Use when a limb is severed / unsealed",
            _ => Type.ToString()
        };

        var target = TargetSelector switch
        {
            MedicalTargetSelector.Auto => "auto target",
            MedicalTargetSelector.MostDamagedPart => "most damaged part",
            MedicalTargetSelector.SeveredOrUnsealedSocket => "unsealed socket",
            MedicalTargetSelector.SpecificPart => TargetPartKey ?? "specific part",
            _ => TargetSelector.ToString()
        };

        return $"{when} ({target})";
    }

    public BodyPart? SelectTarget(Pawn self)
    {
        if (TargetSelector == MedicalTargetSelector.SpecificPart)
        {
            return self.Body.FindPartByKey(TargetPartKey);
        }

        if (TargetSelector == MedicalTargetSelector.SeveredOrUnsealedSocket)
        {
            return FindUnsealedSocket(self)?.ParentPart;
        }

        var parts = self.Body.AllExternalParts
            .OrderBy(p => p.HealthPercent)
            .ToList();

        return parts.FirstOrDefault();
    }

    public static BodyPartSocket? FindUnsealedSocket(Pawn self)
    {
        foreach (var part in self.Body.AllParts)
        {
            foreach (var socket in part.Sockets)
            {
                if (socket.AttachedPart == null && socket.IsSealed == false)
                {
                    return socket;
                }
            }
        }

        var root = self.Body.RootSocket;
        if (root.AttachedPart == null && root.IsSealed == false)
        {
            return root;
        }

        return null;
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref Type, "Type");
        ScribeValues.Look(ref TargetSelector, "TargetSelector");
        ScribeValues.Look(ref Threshold, "Threshold");
        ScribeValues.Look(ref AfterSeconds, "AfterSeconds");
        ScribeValues.Look(ref HealthThreshold, "HealthThreshold", 0.6f);
        ScribeValues.Look(ref TargetPartKey, "TargetPartKey");
    }

    private bool IsSelfPartsDamaged(Pawn self)
    {
        var externalParts = self.Body.AllExternalParts;
        var eyes = externalParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count > 0 && eyes.All(e => !e.IsFunctional))
        {
            return true;
        }

        var healthThreshold = HealthThreshold > 0 ? HealthThreshold : 0.6f;
        var damagedCount = externalParts.Count(p => p.HealthPercent < healthThreshold);
        return damagedCount >= externalParts.Count * Threshold;
    }

    private bool HasPartBelowHealth(Pawn self)
    {
        var healthThreshold = HealthThreshold > 0 ? HealthThreshold : 0.6f;
        if (TargetSelector == MedicalTargetSelector.SpecificPart)
        {
            var part = self.Body.FindPartByKey(TargetPartKey);
            return part != null && part.HealthPercent < healthThreshold;
        }

        return self.Body.AllExternalParts.Any(p => p.HealthPercent < healthThreshold);
    }
}
