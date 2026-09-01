namespace Wendlemire.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class HydraBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        // Torso as root
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.TorsoSocket);
        var torso = pawn.Body.RootSocket.TryAttachPart(Defs.BodyParts.HydraTorso);
        
        // Torso internals
        torso.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        torso.GetSocketsFor(BodyPartType.Intestines)[0].TryAttachPart(Defs.BodyParts.Intestines);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        torso.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);

        // Center Head (HeadOne)
        var headSockets = torso.GetSocketsFor(BodyPartType.Head);
        var headOne = headSockets[0].TryAttachPart(Defs.BodyParts.HydraHeadOne);
        headOne.Equipment[EquipmentSlotType.BuiltIn] = headOne.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("HydraTeeth")!);
        headOne.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        headOne.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        headOne.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);

        // Left Head (HeadTwo)
        var headTwo = headSockets[1].TryAttachPart(Defs.BodyParts.HydraHeadTwo);
        headTwo.Equipment[EquipmentSlotType.BuiltIn] = headTwo.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("HydraTeeth")!);
        headTwo.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        headTwo.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        headTwo.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);

        // Right Head (HeadThree)
        var headThree = headSockets[2].TryAttachPart(Defs.BodyParts.HydraHeadThree);
        headThree.Equipment[EquipmentSlotType.BuiltIn] = headThree.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("HydraTeeth")!);
        headThree.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        headThree.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        headThree.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
    }
}
