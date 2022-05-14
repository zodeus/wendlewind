using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Grafted.Definitions;
using Grafted.Definitions.Loader;
using Grafted.Maths;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui.Widgets.DefWidgets;
using Grafted.Sim.Persistence;
using JetBrains.Annotations;

namespace Grafted.Sim.Entities.Pawns;

public class SkillDef : Def {
    public SkillType SkillType = SkillType.None;
    public ToolType ToolType = ToolType.Invalid;
    public override Type DefUiClass => typeof(SkillDefPanel);
}

public enum SkillType {
    None,
    Arms,
    Trade
}
public class SkillValueRecord {
    public SkillDef Def = null!;
    public int Value;

    public override string ToString() {
        return Def.Moniker + ": " + Value;
    }

    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "Def", xmlRoot.Name);
        Value = ParseHelper.FromString<int>(xmlRoot.FirstChild!.Value!);
    }
}

public class PawnSkills : IExposable, IEnumerable<Skill> {
    public readonly Pawn Pawn;
    private List<Skill> _skills;

    public PawnSkills(Pawn pawn) {
        Pawn = pawn;
        _skills = new List<Skill>();
        foreach (SkillDef def in DefRepository<SkillDef>.Defs) {
            _skills.Add(new Skill(def));
        }
    }

    public void Tick() {
        for (int j = 0; j < _skills.Count; j++) {
            _skills[j].Decay();
        }
    }

    public void Learn(SkillDef def, float xp) {
        GetSkill(def).Learn(xp);
    }

    public Skill GetSkill(SkillDef skillDef) {
        for (int i = 0; i < _skills.Count; i++) {
            if (_skills[i].Def == skillDef) {
                return _skills[i];
            }
        }

        throw new NotSupportedException($"Skill not found: {skillDef}");
    }

    public Skill? GetSkill(ToolType toolType) {
        for (int i = 0; i < _skills.Count; i++) {
            if (_skills[i].Def.ToolType == toolType) {
                return _skills[i];
            }
        }

        return null;
    }

    public IEnumerator<Skill> GetEnumerator() {
        return _skills.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void ExposeData() {
        Scribe_Collections.Look(ref _skills!, "skills", LookMode.Deep);
    }
}

public class Skill : IExposable {
    public const int MinLevel = 0;
    public const int MaxLevel = 20;
    public SkillDef Def = null!;
    public int Level;
    public float CurrentLevelXp;
    public float XpRequiredForLevelUp => XpRequiredToLevelUpFrom(Level);
    public SkillType SkillType => Def.SkillType;

    public float TotalXp {
        get {
            float total = 0f;
            for (int i = 0; i < Level; i++) {
                total += XpRequiredToLevelUpFrom(i);
            }

            total += CurrentLevelXp;

            return total;
        }
    }

    private static readonly Curve XpLevelingCurve = new() {
        new CurvePoint(0f, 50f), // 100 increment
        new CurvePoint(3f, 100f), // 100 increment
        new CurvePoint(5f, 500f), // 200 increment
        new CurvePoint(6f, 500f)
    };

    public Skill() { }

    public Skill(SkillDef def) {
        Def = def;
    }

    public void Decay() {
        //if (Level >= 20) {
        //Learn(-0.005f * decayFactor);
        //}
    }

    public void Learn(float xp) {
        CurrentLevelXp += xp;
        while (CurrentLevelXp >= XpRequiredForLevelUp) {
            CurrentLevelXp -= XpRequiredForLevelUp;
            Level++;
        }

        do {
            if (CurrentLevelXp < 0) {
                Level--;
                CurrentLevelXp += XpRequiredForLevelUp;
                continue;
            }

            return;
        } while (Level > 0);

        Level = 0;
        CurrentLevelXp = 0;
    }

    public static float XpRequiredToLevelUpFrom(int startingLevel) {
        return XpLevelingCurve.Evaluate(startingLevel);
    }

    public void ExposeData() {
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_Values.Look(ref Level, "Level");
        Scribe_Values.Look(ref CurrentLevelXp, "CurrentLevelXp");
    }
}

public class SkillRecord {
    public SkillDef Def = null!;
    public int Value;

    public override string ToString() {
        return Def.Moniker + ": " + Value;
    }

    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "Def", xmlRoot.Name);
        Value = ParseHelper.FromString<int>(xmlRoot.FirstChild!.Value!);
    }
}