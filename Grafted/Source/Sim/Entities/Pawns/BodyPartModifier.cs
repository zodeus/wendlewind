using System;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Persistence;
using JetBrains.Annotations;

namespace Grafted.Sim.Entities.Pawns;

public static class BodyPartModifierGenerator {
    public static BodyPartModifier Generate(BodyPartModifierDef def, int duration) {
        BodyPartModifier modifer = (BodyPartModifier) Activator.CreateInstance(def.HandlerClass)!;
        modifer.Def = def;
        modifer.Id = Core.Sim.IdProvider.NextBodyPartModifierId();
        modifer.DurationInMinutes = duration;
        modifer.Initialize();
        return modifer;
    }
}

public abstract class BodyPartModifier : IExposable, IIdentityProvider {
    public BodyPart BodyPart = null!;
    public BodyPartModifierDef Def = null!;
    public int Ticks;
    public int DurationInMinutes;
    public int Id = -1;
    public bool IsCured;
    public int Severity = 1;

    public string Label => Def.Label;

    public virtual void Tick() {
        Ticks++;
        if (Ticks >= SimTime.TicksPerMinute * DurationInMinutes) {
            IsCured = true;
        }
    }

    public virtual void Initialize() { }

    public void SpreadTo(BodyPart part) {
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Def, SimTime.TicksPerMinute * DurationInMinutes - Ticks));
    }

    public virtual void MergeWith(BodyPartModifier modifier) {
        Log.Warning($"No implementation for merging {modifier.Label} with self={this}");
    }

    public virtual void ExposeData() {
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_References.Look(ref BodyPart!, "BodyPart");
        Scribe_Values.Look(ref Id, "Id");
        Scribe_Values.Look(ref Ticks, "Ticks");
        Scribe_Values.Look(ref DurationInMinutes, "DurationInMinutes");
        Scribe_Values.Look(ref IsCured, "IsCured");
        Scribe_Values.Look(ref Severity, "Severity");
    }

    public string GetUniqueId() {
        return "BodyPartModifier_" + Id;
    }

    public override string ToString() {
        return $"{Def.Moniker} Id: {Id}";
    }
}

public class BodyPartModifierDef : Def {
    [UsedImplicitly] public Type HandlerClass = typeof(BodyPartModifier);
}

public class BodyPartModifierRecord {
    public BodyPartModifierDef Def = null!;
    public RangeInt DurationInMinutes = new RangeInt(0, 0);
    public RangeFloat Chance = RangeFloat.One;
}

[UsedImplicitly]
public class BurningAcid : BodyPartModifier {
    public bool HasSpread = false;

    public override void Tick() {
        BodyPart.HitPoints -= BodyPart.HitPoints * .02f;
        if (HasSpread == false && BodyPart.Type == BodyPartType.Skin && BodyPart.HealthPercent < .2f) {
            HasSpread = true;
            if (BodyPart.Socket?.ParentPart != null) {
                SpreadTo(BodyPart.Socket.ParentPart);
            }
        }

        base.Tick();
    }

    public override void ExposeData() {
        Scribe_Values.Look(ref HasSpread, "HasSpread");
        base.ExposeData();
    }
}