namespace Wendlemire.Sim.Entities.Items.Medicinals;

/// <summary>
/// Installs a second vital heart. Death still requires every Heart to fail.
/// </summary>
[UsedImplicitly]
public class MechanicalHeartHandler : MedicinalHandler
{
    public MechanicalHeartHandler(IRng rng)
    {
        Rng = rng;
    }

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        return part.Body?.Pawn != null && TryInstall(part.Body.Pawn);
    }

    public static bool TryInstall(Pawn pawn)
    {
        var body = pawn.Body;
        if (body.AllParts.Any(p => p.BodyPartDef == Defs.BodyParts.MechanicalHeart))
        {
            return true;
        }

        BodyPart? host = null;
        BodyPartSocket? empty = null;
        foreach (var candidate in body.AllParts)
        {
            foreach (var socket in candidate.GetSocketsFor(BodyPartType.Heart))
            {
                if (socket.AttachedPart != null)
                {
                    continue;
                }

                empty = socket;
                host = candidate;
            }
        }

        if (empty == null)
        {
            host = body.AllParts.FirstOrDefault(p => p.Type == BodyPartType.RibCage)
                   ?? body.AllParts.FirstOrDefault(p => p.GetSocketsFor(BodyPartType.Heart).Count > 0);
            if (host == null || Defs.BodyPartSockets.AuxiliaryHeartSocket == null)
            {
                return false;
            }

            empty = new BodyPartSocket(Defs.BodyPartSockets.AuxiliaryHeartSocket, host)
            {
                Body = body
            };
            host.Sockets.Add(empty);
        }

        empty.TryAttachPart(Defs.BodyParts.MechanicalHeart);
        BodyPart.NotifyStructureChanged(host);
        return true;
    }

    public static void Uninstall(Pawn pawn)
    {
        foreach (var heart in pawn.Body.AllParts.Where(p => p.BodyPartDef == Defs.BodyParts.MechanicalHeart).ToList())
        {
            var socket = heart.Socket;
            var host = socket?.ParentPart;
            if (socket != null)
            {
                socket.AttachedPart = null;
                socket.IsSealed = true;
                heart.Socket = null;
                if (socket.Def == Defs.BodyPartSockets.AuxiliaryHeartSocket && host != null)
                {
                    host.Sockets.Remove(socket);
                }
            }

            BodyPart.NotifyStructureChanged(host);
        }
    }

    public override string GetEffectDescription(Item item) =>
        "Installs a second mechanical heart. One ruined heart no longer kills.";

    public override IReadOnlyList<string> GetHowItWorks(Item item) =>
    [
        "Installs as soon as you slot it in the medical chest.",
        "Grafts a metal heart beside the organic one — it shows on your chest.",
        "You die of heart failure only after every heart fails.",
        "Unslotting it removes the graft. Does not stack."
    ];
}
