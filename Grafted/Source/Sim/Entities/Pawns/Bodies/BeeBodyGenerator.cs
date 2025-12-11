namespace Grafted.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class BeeBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.BeeHead));
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        head.GetSocketsFor(BodyPartType.Antenna)[0].TryAttachPart(Defs.BodyParts.BeeAntenna);
        head.GetSocketsFor(BodyPartType.Antenna)[1].TryAttachPart(Defs.BodyParts.BeeAntenna);
        head.GetSocketsFor(BodyPartType.Minion)[0].TryAttachPart(Defs.BodyParts.BeeDrone);
        head.GetSocketsFor(BodyPartType.Minion)[1].TryAttachPart(Defs.BodyParts.BeeDrone);
        head.GetSocketsFor(BodyPartType.Minion)[2].TryAttachPart(Defs.BodyParts.BeeDrone);
        head.GetSocketsFor(BodyPartType.Minion)[3].TryAttachPart(Defs.BodyParts.BeeDrone);

        // Thorax
        var thorax = head.GetSocketsFor(BodyPartType.Thorax)[0].TryAttachPart(Defs.BodyParts.BeeThorax);
        thorax.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        thorax.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);
        thorax.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.BeeLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.BeeLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[2].TryAttachPart(Defs.BodyParts.BeeLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[3].TryAttachPart(Defs.BodyParts.BeeLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[4].TryAttachPart(Defs.BodyParts.BeeLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[5].TryAttachPart(Defs.BodyParts.BeeLeg);

        // Abdomen
        var abdomen = thorax.GetSocketsFor(BodyPartType.Abdomen)[0].TryAttachPart(Defs.BodyParts.BeeAbdomen);
        abdomen.GetSocketsFor(BodyPartType.Wing)[0].TryAttachPart(Defs.BodyParts.BeeWing);
        abdomen.GetSocketsFor(BodyPartType.Wing)[1].TryAttachPart(Defs.BodyParts.BeeWing);
    }
}