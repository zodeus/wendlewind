namespace Grafted.Sim.Entities.Pawns.Bodies;

public class GlumpBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.TorsoSocket);
        GenerateBodyInSocket(pawn.Body.RootSocket);
    }

    private static void GenerateBodyInSocket(BodyPartSocket rootSocket)
    {
        var torso = rootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.GlumpTorso));
        torso.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        torso.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        //RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.GlumpRibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        ribCage.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);

        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.GlumpArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.GlumpArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.GlumpLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.GlumpLeg));
    }

    static void MakeArm(BodyPart arm)
    {
        arm.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeHand(arm.GetSocketsFor(BodyPartType.Hand)[0].TryAttachPart(Defs.BodyParts.GlumpHand));
    }

    static void MakeHand(BodyPart hand)
    {
        var artery = hand.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        //artery.HitPoints = 0;
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);

        MakeFinger(hand.GetSocketsFor(BodyPartType.Thumb)[0].TryAttachPart(Defs.BodyParts.HumanThumb));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[0].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[1].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[2].TryAttachPart(Defs.BodyParts.HumanFinger));
    }

    static void MakeFinger(BodyPart finger)
    {
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        finger.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
    }

    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);

        var foot = leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.GlumpFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        foot.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
    }
}