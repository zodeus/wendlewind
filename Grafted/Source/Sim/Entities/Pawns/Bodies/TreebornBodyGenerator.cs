namespace Grafted.Sim.Entities.Pawns.Bodies;

public class TreebornBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.TreeTrunkSocket);
        GenerateBodyInSocket(pawn.Body.RootSocket);
    }

    private static void GenerateBodyInSocket(BodyPartSocket rootSocket)
    {
        var torso = rootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.TreeTrunk));
        torso.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        torso.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);

        //RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.TreeInnerCore);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[2].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[3].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[4].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[5].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[6].TryAttachPart(Defs.BodyParts.TreeStump));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[7].TryAttachPart(Defs.BodyParts.TreeStump));
    }


    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        leg.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("TreeBranch")!);
    }
}