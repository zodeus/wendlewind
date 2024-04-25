using Grafted.Sim.Entities;

namespace Grafted.Sim.Combat;

public static class CombatHelpers
{
    public static DamageRequest CalculateDamages(Pawn pawn, Item tool)
    {
        var pawnStrength = pawn.GetStatValue(Defs.Stats.MeleeStrength);
        var toolPower = tool.GetStatValue(Defs.Stats.MeleePower);
        var toolManeuver = tool.ItemDef.ToolManeuvers.RandomElement();
        var skillPower = 1 + (pawn.GetSkill(tool.ItemDef.ToolType)?.Level *.1f ?? 0);
        var rawDamage = Mathf.RoundToInt(
            toolPower
            * pawnStrength
            * skillPower
            * toolManeuver.DamageMultiplier.RandomValue
        );
        //rawDamage *= tool.GetStatValue(Defs.Stats.WeaponDamageMultiplier);
        if (rawDamage < 1)
        {
            rawDamage = 1;
        }

        DamageRequest request = new(pawn, tool, toolManeuver);
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

public class Damage
{
    public readonly Item Tool;
    public ToolType ToolType => Tool.ItemDef.ToolType;
    public DamageType Type => Tool.ItemDef.WeaponProperties.DamageType;
    public List<BodyPartModifierRecord> BodyPartModifiers => Tool.ItemDef.WeaponProperties.BodyPartModifiers;

    public readonly int Amount;
    public int UnblockedAmount;

    public Damage(Item tool, int amount)
    {
        Tool = tool;
        Amount = amount;
        UnblockedAmount = amount;
    }
}

public class DamageRequest
{
    public readonly Pawn Source;
    public Item Tool { get; }
    public List<Damage> RawDamages = new(1);

    public ToolManeuverDef ToolManeuver { get; }

    //public List<HealthConditionDef>? HealthConditions;
    public float TotalRawDamage => RawDamages.Sum(damage => damage.Amount);
    //public bool InflictsConditions => HealthConditions?.Any() ?? false;

    public DamageRequest(Pawn source, Item tool, ToolManeuverDef toolManeuver)
    {
        ToolManeuver = toolManeuver;
        Source = source;
        Tool = tool;
    }
}

public class DamageResponse
{
    public List<DamageRecord> Damages = new();

    //public List<HealthConditionDef>? HealthConditions;
    public bool Killed;
    public bool Dodged;
    public DeathRecord? DeathRecord;

    public DamageResponse()
    {
    }
}

public class DestroyedItemRecord
{
    public readonly ItemDef Def;

    public DestroyedItemRecord(ItemDef def)
    {
        Def = def;
    }
}

public class DamageRecord
{
    public readonly DamageType DamageType;
    public readonly BodyPart BodyPartHit;
    public IReadOnlyList<DamagedPartRecord> BodyParts = new List<DamagedPartRecord>();
    public List<DestroyedItemRecord> DestroyedEquipment = new();
    public readonly float RawAmount;
    public float ActualAmount;

    public DamageRecord(DamageType damageType, BodyPart bodyPartHit, float rawAmount)
    {
        DamageType = damageType;
        BodyPartHit = bodyPartHit;
        RawAmount = rawAmount;
    }

    public float AmountBlocked => RawAmount - ActualAmount;
}

public class DamagedPartRecord
{
    public readonly BodyPart BodyPart;
    public float Amount;
    public string Label => BodyPart.Label;
    public List<BodyPartModifierDef> AppliedModifiers = new();
    public BodyPartType PartType => BodyPart.Type;
    public bool WasDestroyed => BodyPart.IsDestroyed;
    public bool WasSevered => BodyPart.IsSevered;
    public bool IsVital => BodyPart.IsVital;

    public DamagedPartRecord(BodyPart bodyPart)
    {
        BodyPart = bodyPart;
    }
}