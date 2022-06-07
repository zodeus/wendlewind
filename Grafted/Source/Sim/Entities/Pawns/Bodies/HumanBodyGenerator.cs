using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns.Bodies;

public class HumanBodyGenerator : IBodyGenerator {
    public BodyPartSocket Generate() {
        return GenerateBody();
    }

    private static BodyPartSocket GenerateBody() {
        BodyPartSocket rootSocket = new(Defs.BodyPartSockets.HeadSocket);
        BodyPart head = rootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.HumanHead));
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        //Skull
        BodyPart skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Neck
        BodyPart neck = head.GetSocketsFor(BodyPartType.Neck)[0].TryAttachPart(Defs.BodyParts.HumanNeck);
        neck.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        neck.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        neck.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        //Torso
        BodyPart torso = neck.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.HumanTorso);
        //torso.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);

        //RibCage
        BodyPart ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.RibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);
        
        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.HumanArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.HumanArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.HumanLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.HumanLeg));

        return rootSocket;
    }

    static void MakeArm(BodyPart arm) {
        arm.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeHandForSocket(arm.GetSocketsFor(BodyPartType.Hand)[0]);
    }


    public static void MakeHandForSocket(BodyPartSocket socket) {
        BodyPart hand = socket.TryAttachPart(Defs.BodyParts.HumanHand);
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        hand.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Thumb)[0], Defs.BodyParts.HumanThumb);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[0], Defs.BodyParts.HumanFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[1], Defs.BodyParts.HumanFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[2], Defs.BodyParts.HumanFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[3], Defs.BodyParts.HumanFinger);
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);
    }

    public static void MakeFingerForSocket(BodyPartSocket socket, BodyPartDef def) {
        BodyPart finger = socket.TryAttachPart(def);
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        finger.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
    }

    static void MakeLeg(BodyPart leg) {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        BodyPart foot = leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.HumanFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);

        foot.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
    }
}