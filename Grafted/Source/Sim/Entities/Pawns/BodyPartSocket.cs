namespace Grafted.Sim.Entities.Pawns;

public class AdaptiveBodyPartProperties {
    public MaxHitPointScaler MaxHitPointScaler = null!;
}

public abstract class MaxHitPointScaler {
    public abstract float GetMaxHitPointsFor(BodyPart parentPart);
}

class MaxHitPointScalerConstantFactor : MaxHitPointScaler {
    public float Factor = 0;

    public override float GetMaxHitPointsFor(BodyPart parentPart) {
        return Math.Max(1, parentPart.MaxHitPoints * Factor);
    }
}

class MaxHitPointScalerCurve : MaxHitPointScaler {
    public SimpleCurve SimpleCurve = null!;

    public override float GetMaxHitPointsFor(BodyPart parentPart) {
        return SimpleCurve.Evaluate(parentPart.MaxHitPoints);
    }
}

public class BodyPartSocket : IExposable, IIdentityProvider {
    public PawnBody? Body;
    public BodyPartSocketDef Def = null!;
    public BodyPart? AttachedPart;
    public BodyPart? ParentPart;
    public bool IsSealed = false;
    public int Id;
    public static int NEXT_SOCKET_ID = 1; //todo

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
        ScribeValues.Look(ref Id!, "Id");
        ScribeDefs.Look(ref Def!, "Def");
        ScribeDeep.Look(ref AttachedPart!, "AttachedPart");
        ScribeReferences.Look(ref ParentPart!, "ParentPart");
        ScribeReferences.Look(ref Body, "Body");
        ScribeValues.Look(ref IsSealed, "IsSealed");
        ScribeValues.Look(ref NEXT_SOCKET_ID, "NEXT_SOCKET_ID");
    }
}