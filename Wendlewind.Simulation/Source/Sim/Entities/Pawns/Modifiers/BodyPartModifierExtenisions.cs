namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

public static class BodyPartModifierExtensions
{
    public static void HandleSpreading(this BodyPartModifier modifier, BodyPart bodyPart, double spreadThreshold, ref bool hasSpread)
    {
        if (hasSpread) return;
        if (bodyPart.HealthPercent > spreadThreshold) return;
        hasSpread = true;

        BodyPart? rootExternalPart;
        if (bodyPart.IsExternal)
        {
            rootExternalPart = bodyPart;
        }
        else
        {
            rootExternalPart = bodyPart.Socket?.ParentPart;
            if (rootExternalPart == null)
            {
                Log.Warning($"{modifier.Def.Moniker}: Root external part is not external: {bodyPart.Label}");
                return;
            }
        }

        if (rootExternalPart.Socket?.ParentPart is { } parentPart)
        {
            modifier.SpreadViaSkinIfPossible(parentPart);
        }

        foreach (var childPart in rootExternalPart.ExternalParts)
        {
            modifier.SpreadViaSkinIfPossible(childPart);
        }
    }

    public static void SpreadViaSkinIfPossible(this BodyPartModifier modifier, BodyPart part)
    {
        var skin = part.Skin;
        if (skin != null)
        {
            modifier.SpreadTo(skin);
        }
        else
        {
            modifier.SpreadTo(part);
        }
    }

    public static void HandlePenetration(this BodyPartModifier modifier, BodyPart bodyPart, double penetrationThreshold, ref bool hasPenetrated)
    {
        if (hasPenetrated) return;
        if (bodyPart.HealthPercent > penetrationThreshold) return;

        var rootPart = bodyPart;
        if (rootPart.Type == BodyPartType.Skin && bodyPart.Socket?.ParentPart != null)
        {
            modifier.SpreadTo(bodyPart.Socket.ParentPart);
            rootPart = bodyPart.Socket.ParentPart;
        }

        foreach (var internalPart in rootPart.InternalParts)
        {
            modifier.SpreadTo(internalPart);
        }

        hasPenetrated = true;
    }
}