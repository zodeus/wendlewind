using System;
using Grafted.Maths;
using Grafted.Sim.Persistence;
using JetBrains.Annotations;

namespace Grafted.Sim.Entities.Pawns;

public class AdaptiveBodyPartProperties {
    public HitPointScaler HitPointScaler = null!;
}

public abstract class HitPointScaler {
    public abstract float GetHitPointsFor(BodyPart parentPart);
}

class HitPointScalerConstantFactor : HitPointScaler {
    public float Factor = 0;

    public override float GetHitPointsFor(BodyPart parentPart) {
        return Math.Max(1, parentPart.HitPoints * Factor);
    }
}

class HitPointScalerCurve : HitPointScaler {
    public Curve Curve = null!;

    public override float GetHitPointsFor(BodyPart parentPart) {
        return Curve.Evaluate(parentPart.HitPoints);
    }
}

public class BodyPartSocket : IExposable, IIdentityProvider {
    public PawnBody? Body;
    public BodyPartSocketDef Def = null!;
    public BodyPart? AttachedPart;
    public BodyPart? ParentPart;
    public bool IsSealed = false;
    public int Id;
    public static int NEXT_SOCKET_ID = 1;

    public BodyPartPosition? Position => Def.Position ?? ParentPart?.Position;

    public bool IsExternal => Def.IsExternal;

    public string Label => Def.Label;

    [UsedImplicitly]
    public BodyPartSocket() { }

    public BodyPartSocket(BodyPartSocketDef def, BodyPart? parentPart = null) {
        Def = def;
        ParentPart = parentPart;
        Id = NEXT_SOCKET_ID++;
    }

    public BodyPart TryAttachPart(BodyPartDef def) {
        return TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(def));
    }

    public BodyPart TryAttachPart(BodyPart bodyPart) {
        if (CanSocket(bodyPart.Type) == false) {
            throw new NotImplementedException();
        }

        AttachedPart = bodyPart;
        bodyPart.Socket = this;
        IsSealed = true;

        bodyPart.AdaptBodyPartTo(ParentPart);

        return bodyPart;
    }

    public bool CanSocket(BodyPartType bodyPartType) {
        return Def.AllowedBodyPartTypes.Contains(bodyPartType);
    }

    public override string ToString() {
        return Def.Moniker;
    }

    public string GetUniqueId() {
        return $"{GetType().Name}-{Id}";
    }

    public void ExposeData() {


        Scribe_Values.Look(ref Id!, "Id");
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_Deep.Look(ref AttachedPart!, "AttachedPart");
        Scribe_References.Look(ref ParentPart!, "ParentPart");
        Scribe_References.Look(ref Body, "Body");
        Scribe_Values.Look(ref IsSealed, "IsSealed");
        Scribe_Values.Look(ref NEXT_SOCKET_ID, "NEXT_SOCKET_ID");
    }
}