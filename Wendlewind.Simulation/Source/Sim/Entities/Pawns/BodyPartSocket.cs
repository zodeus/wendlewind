namespace Wendlewind.Sim.Entities.Pawns;

public class AdaptiveBodyPartProperties
{
    public MaxHitPointScaler MaxHitPointScaler = null!;
}

public abstract class MaxHitPointScaler
{
    public abstract float GetMaxHitPointsFor(BodyPart parentPart);
}

class MaxHitPointScalerConstantFactor : MaxHitPointScaler
{
    public float Factor = 0;

    public override float GetMaxHitPointsFor(BodyPart parentPart)
    {
        return Math.Max(1, (float)parentPart.MaxHitPoints * Factor);
    }
}

class MaxHitPointScalerCurve : MaxHitPointScaler
{
    public SimpleCurve SimpleCurve = null!;

    public override float GetMaxHitPointsFor(BodyPart parentPart)
    {
        return SimpleCurve.Evaluate((float)parentPart.MaxHitPoints);
    }
}

public class BodyPartSocket : IExposable, IIdentityProvider
{
    public PawnBody? Body;
    public BodyPartSocketDef Def = null!;
    public BodyPart? AttachedPart;
    public BodyPart? ParentPart;
    public bool IsSealed;
    public int Id;
    public static int NextSocketId = 1; //todo

    public BodyPartPosition? Position => Def.Position ?? ParentPart?.Position;

    public bool IsExternal => Def.IsExternal;

    public string Label => Def.Label;

    [UsedImplicitly]
    public BodyPartSocket()
    {
    }

    public BodyPartSocket(BodyPartSocketDef def, BodyPart? parentPart = null)
    {
        Def = def;
        ParentPart = parentPart;
        Id = NextSocketId++;
    }

    public BodyPart TryAttachPart(BodyPartDef def)
    {
        var context = ParentPart?.Context ?? Body?.Pawn.Context
                      ?? throw new InvalidOperationException("BodyPartSocket has no GameContext to create a part.");
        return TryAttachPart(context.Factory.CreateEntity<BodyPart>(def));
    }

    public BodyPart TryAttachPart(BodyPart bodyPart)
    {
        if (CanSocket(bodyPart.Type) == false)
        {
            Log.Error($"Cannot socket part {bodyPart.Label} to {Label} because it is not allowed");
            throw new NotImplementedException();
        }

        AttachedPart = bodyPart;
        bodyPart.Socket = this;
        IsSealed = true;

        bodyPart.AdaptBodyPartTo(ParentPart);
        BodyPart.NotifyStructureChanged(ParentPart ?? bodyPart);
        Body?.InvalidatePartCaches();

        return bodyPart;
    }

    public bool CanSocket(BodyPartType bodyPartType)
    {
        return Def.AllowedBodyPartTypes.Contains(bodyPartType);
    }

    public override string ToString()
    {
        return Def.Moniker;
    }

    public string GetUniqueId()
    {
        return $"{GetType().Name}-{Id}";
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref Id, "Id");
        ScribeDefs.Look(ref Def!, "Def");
        ScribeDeep.Look(ref AttachedPart!, "AttachedPart");
        ScribeReferences.Look(ref ParentPart!, "ParentPart");
        ScribeReferences.Look(ref Body, "Body");
        ScribeValues.Look(ref IsSealed, "IsSealed");
        ScribeValues.Look(ref NextSocketId, "NEXT_SOCKET_ID");
    }
}