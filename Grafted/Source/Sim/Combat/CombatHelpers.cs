using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public static class CombatHelpers {
    public static List<CombatSequence> GetPotentialCombatSequencesFor(this Pawn pawn, IEnumerable<Item> tools, int availableSequencePoints, Pawn target) {
        List<CombatSequence> combatSequences = new();
        foreach (Item tool in tools) {
            foreach (CombatSequence sequence in GetSequencesForTool(pawn, tool, availableSequencePoints, target)) {
                combatSequences.Add(sequence);
            }
        }

        return combatSequences;
    }

    private static IEnumerable<CombatSequence> GetSequencesForTool(Pawn pawn, Item tool, int availableSequencePoints, Pawn target) {
        foreach (ToolSequenceDef toolSequence in tool.ItemDef.ToolSequences) {
            if (toolSequence.TotalSequencePoints > availableSequencePoints) {
                continue;
            }

            yield return GetToolSequence(pawn, tool, target, toolSequence);
        }
    }

    public static CombatSequence GetToolSequence(Pawn pawn, Item tool, Pawn target, ToolSequenceDef toolSequence) {
        return new CombatSequence {
            Source = pawn,
            TotalSequencePoints = toolSequence.TotalSequencePoints,
            Target = target,
            FlavorText = toolSequence.Label,
            Steps = GetSteps(pawn, tool, toolSequence)
        };
    }

    private static List<CombatSequenceStep> GetSteps(Pawn pawn, Item tool, ToolSequenceDef toolSequence) {
        List<CombatSequenceStep> steps = new();
        foreach (ToolManeuverDef maneuver in toolSequence.Maneuvers) {
            steps.Add(new CombatSequenceStep {
                Name = maneuver.Label,
                Tool = tool.Label,
                Damages = CalculateDamages(pawn, tool, toolSequence, maneuver)
            });
        }

        return steps;
    }

    private static DamageRequest CalculateDamages(Pawn pawn, Item tool, ToolSequenceDef sequence, ToolManeuverDef maneuver) {
        float pawnStrength = pawn.GetStatValue(Defs.Stats.MeleeStrength);
        float toolPower = tool.GetStatValue(Defs.Stats.MeleePower);
        float skillPower = 1 + (pawn.GetSkill(tool.ItemDef.ToolType)?.Level / 30f ?? 0);
        int rawDamage = Mathf.RoundToInt(
            maneuver.DamageMultiplier.RandomValue
            * sequence.DamageMultiplier.RandomValue
            * toolPower
            * pawnStrength
            * skillPower
        );
        //rawDamage *= tool.GetStatValue(Defs.Stats.WeaponDamageMultiplier);
        if (rawDamage < 1) {
            rawDamage = 1;
        }

        DamageRequest request = new(pawn);
        request.RawDamages.Add(new Damage(tool, rawDamage));
        //todo
        /*
        if (tool.ItemDef.InflictableHealthConditions != null) {
            request.HealthConditions = new List<HealthConditionDef>();
            foreach (InflictableHealthConditionRecord condition in tool.ItemDef.InflictableHealthConditions) {
                if (Core.Random.Chance(condition.ChanceToInflict.RandomValue)) {
                    request.HealthConditions.Add(condition.Condition);
                }
        }
        }*/

        return request;
    }
}

public class Damage {
    public readonly Item Tool;
    public ToolType ToolType => Tool.ItemDef.ToolType;
    public DamageType Type => Tool.ItemDef.WeaponProperties.DamageType;
    public List<BodyPartModifierRecord> BodyPartModifiers => Tool.ItemDef.WeaponProperties.BodyPartModifiers;

    public readonly int Amount;
    public int UnblockedAmount;

    public Damage(Item tool, int amount) {
        Tool = tool;
        Amount = amount;
        UnblockedAmount = amount;
    }
}

public class DamageRequest {
    public readonly Pawn Source;
    public List<Damage> RawDamages = new(1);

    public DamageRequest(Pawn source) {
        Source = source;
    }
    //public List<HealthConditionDef>? HealthConditions;

    public float TotalRawDamage => RawDamages.Sum(damage => damage.Amount);
    //public bool InflictsConditions => HealthConditions?.Any() ?? false;
}

public class DamageResponse {
    public List<DamageRecord> Damages = new();
    //public List<HealthConditionDef>? HealthConditions;
    public bool Killed;
    public bool Dodged;

    public DamageResponse() { }
}

public class DestroyedItemRecord {
    public readonly ItemDef Def;

    public DestroyedItemRecord(ItemDef def) {
        Def = def;
    }
}

public class DamageRecord {
    public readonly DamageType DamageType;
    public readonly BodyPart BodyPartHit;
    public IReadOnlyList<DamagedPartRecord> BodyParts = new List<DamagedPartRecord>();
    public List<DestroyedItemRecord> DestroyedEquipment = new();
    public readonly float RawAmount;
    public float ActualAmount;

    public DamageRecord(DamageType damageType, BodyPart bodyPartHit, float rawAmount) {
        DamageType = damageType;
        BodyPartHit = bodyPartHit;
        RawAmount = rawAmount;
    }

    public float AmountBlocked => RawAmount - ActualAmount;
}

public class DamagedPartRecord {
    public readonly BodyPart BodyPart;
    public float Amount;
    public string Label => BodyPart.Label;
    public List<BodyPartModifierDef> AppliedModifiers = new();
    public BodyPartType PartType => BodyPart.Type;
    public bool WasDestroyed => BodyPart.IsDestroyed;
    public bool WasSevered => BodyPart.IsSevered;
    public bool IsVital => BodyPart.IsVital;

    public DamagedPartRecord(BodyPart bodyPart) {
        BodyPart = bodyPart;
    }
}