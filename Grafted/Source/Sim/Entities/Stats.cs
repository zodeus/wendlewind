using System;
using System.Collections.Generic;
using System.Xml;
using Grafted.Definitions;
using Grafted.Definitions.Loader;
using Grafted.Maths;
using JetBrains.Annotations;

namespace Grafted.Sim.Entities;

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
    public Type HandlerClass = typeof(StatHandler);

    private StatHandler? _handler;

    public StatHandler Handler {
        get {
            if (_handler == null) {
                _handler = (StatHandler) Activator.CreateInstance(HandlerClass, this)!;
            }

            return _handler;
        }
    }

    public List<StatDef>? StatFactors;
}

public static class StatExtensions {
    public static float GetStatValue(this Entity entity, StatDef stat) {
        return stat.Handler.GetValue(entity);
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

public class StatHandler {
    private readonly StatDef _stat;

    public StatHandler(StatDef stat) {
        _stat = stat;
    }

    public virtual float GetValue(Entity entity) {
        float value = GetBaseValue(entity.Def);
        if (_stat.StatFactors != null) {
            for (int i = 0; i < _stat.StatFactors.Count; i++) {
                value *= entity.GetStatValue(_stat.StatFactors[i]);
            }
        }

        value = Mathf.Clamp(value, _stat.MinValue, _stat.MaxValue);
        return value;
    }

    private float GetBaseValue(EntityDef def) {
        float result = _stat.BaseValue;
        for (int i = 0; i < def.BaseStats.Count; i++) {
            if (def.BaseStats[i].Def != _stat)
                continue;
            result = def.BaseStats[i].Value;
            break;
        }

        return result;
    }
}