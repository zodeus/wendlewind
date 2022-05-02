using System;
using Grafted.Maths;

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

public class BodyPartSocket {
    public BodyPartSocketDef Def;
    public BodyPart? AttachedPart;
    public BodyPart? ParentPart;
    public bool IsSealed = false;

    public BodyPartPosition? Position => Def.Position ?? ParentPart?.Position;

    public bool IsExternal => Def.IsExternal;

    public string Label => Def.Label;

    public BodyPartSocket(BodyPartSocketDef def, BodyPart? parentPart = null) {
        Def = def;
        ParentPart = parentPart;
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
}