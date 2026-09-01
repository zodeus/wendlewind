namespace Wendlemire.Sim.Entities.Items.Medicinals;

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

    public bool ShouldFire(Pawn self, Pawn enemy, int tick, ItemDef? def = null)
    {
        var props = def?.MedicinalProperties;
        return Type switch
        {
            MedicalTriggerType.Immediately => true,
            MedicalTriggerType.AfterSeconds => tick >= AfterSeconds * GameContext.TicksPerSecond,
            MedicalTriggerType.SelfBloodBelow => self.Body.BloodPercent < Threshold,
            MedicalTriggerType.SelfPartsDamaged => self.Body.IsSelfPartsDamaged(Threshold, HealthThreshold),
            MedicalTriggerType.PartBelowHealth => HasPartBelowHealth(self, props),
            MedicalTriggerType.PartSevered => FindUnsealedSocket(self) != null,
            MedicalTriggerType.HasNecrosis => HasStatus(self, props, HasUntreatedNecrosis),
            MedicalTriggerType.BurningOrAcid => HasStatus(self, props, HasBurningOrAcid),
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
            MedicalTriggerType.HasNecrosis => "Use when a part has necrosis",
            MedicalTriggerType.BurningOrAcid => "Use when a part is burning or acid-burned",
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

    public IEnumerable<BodyPart> EnumerateApplyTargets(Pawn self, ItemDef def)
    {
        var props = def.MedicinalProperties;
        var applyMode = props?.ApplyMode ?? MedicalApplyMode.WatchedPart;
        if (applyMode == MedicalApplyMode.Self)
        {
            var root = self.Body.RootSocket.AttachedPart;
            if (root != null)
            {
                yield return root;
            }

            yield break;
        }

        if (TargetSelector == MedicalTargetSelector.SpecificPart)
        {
            var applied = new HashSet<BodyPart>();
            foreach (var candidate in ResolveTargetParts(self, TargetPartKey).OrderBy(p => p.HealthPercent))
            {
                var apply = ResolveApplyPart(candidate, applyMode);
                if (apply != null && applied.Add(apply))
                {
                    yield return apply;
                }
            }

            yield break;
        }

        var watched = CollectWatchParts(self, props, forHealthScan: true)
            .OrderBy(p => p.HealthPercent)
            .ToList();

        var seen = new HashSet<BodyPart>();
        foreach (var candidate in watched)
        {
            var apply = ResolveApplyPart(candidate, applyMode);
            if (apply != null && seen.Add(apply))
            {
                yield return apply;
            }
        }
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

    public static bool CanSealSocket(ItemDef? def)
    {
        return def?.MedicinalProperties?.Watches(MedicalTargetPool.Socket) == true;
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

    public static IEnumerable<BodyPart> CollectWatchParts(Pawn self, MedicinalProperties? props, bool forHealthScan)
    {
        foreach (var part in ListMatchingParts(self, props))
        {
            if (forHealthScan && (part.Type == BodyPartType.Eye || part.HealthPercent >= 1))
            {
                continue;
            }

            yield return part;
        }
    }

    public static List<BodyPart> ListMatchingParts(Pawn self, MedicinalProperties? props)
    {
        var pool = props?.GetWatchPool() ?? [MedicalTargetPool.External];
        var parts = new List<BodyPart>();
        foreach (var part in self.Body.AllParts)
        {
            if (part.IsSevered || part.IsDestroyed || IsOmittedFromTargeting(part.Type))
            {
                continue;
            }

            if (MatchesPool(part, pool))
            {
                parts.Add(part);
            }
        }

        return parts;
    }

    public static List<BodyPart> ListSelectableParts(Pawn self, MedicinalProperties? props)
    {
        var seenPairs = new HashSet<BodyPartType>();
        var picks = new List<BodyPart>();
        foreach (var part in ListMatchingParts(self, props))
        {
            if (IsPairedOrgan(part.Type) && !seenPairs.Add(part.Type))
            {
                continue;
            }

            picks.Add(part);
        }

        return picks;
    }

    public static bool UsesRegionGroups(MedicinalProperties? props)
    {
        return props?.Watches(MedicalTargetPool.Internal) == true;
    }

    public static bool IsOmittedFromTargeting(BodyPartType type)
    {
        return type is BodyPartType.Skin or BodyPartType.Finger or BodyPartType.Thumb;
    }

    public static bool IsPairedOrgan(BodyPartType type)
    {
        return type is BodyPartType.Lung or BodyPartType.Kidney;
    }

    public static string GroupKey(BodyPart part)
    {
        return IsPairedOrgan(part.Type) ? part.Type.ToString() : part.InternalLabel;
    }

    public static string GroupLabel(BodyPart part)
    {
        return part.Type switch
        {
            BodyPartType.Lung => "Lungs",
            BodyPartType.Kidney => "Kidneys",
            _ => part.Label
        };
    }

    public static IReadOnlyList<BodyPart> ResolveTargetParts(Pawn self, string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return [];
        }

        if (Enum.TryParse<BodyPartType>(key, out var type) && IsPairedOrgan(type))
        {
            return LivingPartsOfType(self, type);
        }

        var named = self.Body.FindPartByKey(key);
        if (named != null && IsPairedOrgan(named.Type))
        {
            return LivingPartsOfType(self, named.Type);
        }

        return named != null ? [named] : [];
    }

    private static List<BodyPart> LivingPartsOfType(Pawn self, BodyPartType type)
    {
        var parts = new List<BodyPart>();
        foreach (var part in self.Body.AllParts)
        {
            if (part.Type == type && !part.IsSevered && !part.IsDestroyed)
            {
                parts.Add(part);
            }
        }

        return parts;
    }

    public static string RegionGroupLabel(BodyPart part)
    {
        var current = part;
        var fallback = part;
        while (current != null)
        {
            if (IsRegionRoot(current.Type))
            {
                return current.Type switch
                {
                    BodyPartType.Leg or BodyPartType.Foot or BodyPartType.Hoof => "Legs",
                    BodyPartType.Arm or BodyPartType.Hand or BodyPartType.Paw => "Arms",
                    _ => current.Label
                };
            }

            fallback = current;
            current = current.Socket?.ParentPart;
        }

        return fallback.Label;
    }

    private static bool IsRegionRoot(BodyPartType type)
    {
        return type is BodyPartType.Head
            or BodyPartType.Torso
            or BodyPartType.Arm
            or BodyPartType.Leg
            or BodyPartType.Thorax
            or BodyPartType.Abdomen
            or BodyPartType.Tail
            or BodyPartType.Wing;
    }

    private bool HasPartBelowHealth(Pawn self, MedicinalProperties? props)
    {
        var healthThreshold = HealthThreshold > 0 ? HealthThreshold : 0.6f;
        if (TargetSelector == MedicalTargetSelector.SpecificPart)
        {
            return ResolveTargetParts(self, TargetPartKey).Any(p => p.HealthPercent < healthThreshold);
        }

        return CollectWatchParts(self, props, forHealthScan: true)
            .Any(p => p.HealthPercent < healthThreshold);
    }

    private bool HasStatus(Pawn self, MedicinalProperties? props, Func<BodyPart, bool> predicate)
    {
        if (TargetSelector == MedicalTargetSelector.SpecificPart)
        {
            return ResolveTargetParts(self, TargetPartKey).Any(predicate);
        }

        return CollectWatchParts(self, props, forHealthScan: false).Any(predicate);
    }

    private static bool HasUntreatedNecrosis(BodyPart part)
    {
        return part.HasModifier(Defs.BodyPartModifiers.Necrosis)
               && part.HasModifier(Defs.BodyPartModifiers.NecrosisSerum) == false;
    }

    private static bool HasBurningOrAcid(BodyPart part)
    {
        return part.HasModifier(Defs.BodyPartModifiers.Burning)
               || part.HasModifier(Defs.BodyPartModifiers.Acid);
    }

    private static bool MatchesPool(BodyPart part, IReadOnlyList<MedicalTargetPool> pool)
    {
        for (var i = 0; i < pool.Count; i++)
        {
            if (MatchesPool(part, pool[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPool(BodyPart part, MedicalTargetPool pool)
    {
        return pool switch
        {
            MedicalTargetPool.External => part.IsExternal,
            MedicalTargetPool.Internal => !part.IsExternal && part.IsOrgan,
            MedicalTargetPool.Artery => part.Type == BodyPartType.Artery,
            MedicalTargetPool.Bone => part.Substance == SubstanceType.Bone,
            _ => false
        };
    }

    private static BodyPart? ResolveApplyPart(BodyPart? watched, MedicalApplyMode applyMode)
    {
        if (watched == null)
        {
            return null;
        }

        if (applyMode != MedicalApplyMode.NearestExternalAncestor || watched.IsExternal)
        {
            return watched;
        }

        var current = watched;
        while (current != null && !current.IsExternal)
        {
            current = current.Socket?.ParentPart;
        }

        return current ?? watched.Body?.RootSocket.AttachedPart ?? watched;
    }
}
