using System.Xml;
using Wendlemire.Definitions.Loader;

namespace Wendlemire.Sim.Entities;

public class BaseStat {
    public StatDef Def = null!;
    public float Value;

    public override string ToString() {
        return Def.Moniker + ": " + Value;
    }

    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "Def", xmlRoot.Name);
        Value = ParseHelper.FromString<float>(xmlRoot.FirstChild!.Value!);
    }
}

public class StatDef : Def {
    public float BaseValue;
    public float MinValue;
    public float MaxValue = float.MaxValue;
    public bool UiDisplay;
    public Type HandlerClass = typeof(DefaultStatHandler);

    private DefaultStatHandler? _handler;

    public DefaultStatHandler CreateHandler(ISimFactory factory) =>
        _handler ??= factory.Create<DefaultStatHandler>(HandlerClass, this);

    public List<StatDef>? StatFactors;
}

public static class StatExtensions {
    public static float GetStatValue(this Entity entity, StatDef stat) {
        return stat.CreateHandler(entity.Context.Factory).GetValue(entity);
    }

    public static float GetStatValue(this Pawn pawn, StatDef stat) {
        var value = stat.CreateHandler(pawn.Context.Factory).GetValue(pawn);
        foreach (BodyEffect effect in pawn.Body.Effects)
        {
            effect.ModifyIfApplicable(stat, ref value);
        }
        
        foreach (var equipment in pawn.Equipment.Armor)
        {
            equipment.EquipmentHandler?.ModifyStat(pawn, stat, ref value);
        }
        
        pawn.Body.ModifyStat(stat, ref value);
        return value;
    }

    public static float GetStatFactorFromList(this List<BaseStat>? stats, StatDef stat) {
        return stats?.GetStatValueFromList(stat) ?? 1f;
    }

    public static float GetStatOffsetFromList(this List<BaseStat>? stats, StatDef stat) {
        return stats?.GetStatValueFromList(stat) ?? 0f;
    }

    public static float? GetStatValueFromList(this List<BaseStat> stats, StatDef stat) {
        for (int i = 0; i < stats.Count; i++) {
            if (stats[i].Def == stat) {
                return stats[i].Value;
            }
        }

        return null;
    }
}

public class DefaultStatHandler(StatDef stat)
{
    protected readonly StatDef Stat = stat;

    public virtual float GetValue(Entity entity) {
        var value = GetBaseValue(entity.Def);
        if (Stat.StatFactors != null) {
            // Stat*stat multiplier
            for (var i = 0; i < Stat.StatFactors.Count; i++) {
                value *= entity.GetStatValue(Stat.StatFactors[i]);
            }
        }

        value = Mathf.Clamp(value, Stat.MinValue, Stat.MaxValue);
        return value;
    }

    protected float GetBaseValue(EntityDef def) {
        var result = Stat.BaseValue;
        for (var i = 0; i < def.BaseStats.Count; i++) {
            if (def.BaseStats[i].Def != Stat) continue;
            result = def.BaseStats[i].Value;
            break;
        }

        return result;
    }
}