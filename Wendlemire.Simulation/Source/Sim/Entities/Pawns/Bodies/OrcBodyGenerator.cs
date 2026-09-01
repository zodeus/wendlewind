namespace Wendlemire.Sim.Entities.Pawns.Bodies;

public class OrcBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(Defs.BodyParts.OrcHead);
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        //Skull
        var skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        //Torso
        var torso = head.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.OrcTorso);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);
        torso.GetSocketsFor(BodyPartType.Liver)[0].TryAttachPart(Defs.BodyParts.Liver);
        torso.GetSocketsFor(BodyPartType.Kidney)[0].TryAttachPart(Defs.BodyParts.Kidney);
        torso.GetSocketsFor(BodyPartType.Kidney)[1].TryAttachPart(Defs.BodyParts.Kidney);
        torso.GetSocketsFor(BodyPartType.Spleen)[0].TryAttachPart(Defs.BodyParts.Spleen);
        torso.GetSocketsFor(BodyPartType.Intestines)[0].TryAttachPart(Defs.BodyParts.Intestines);

        //RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.RibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);

        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.OrcArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.OrcArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.OrcLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.OrcLeg));
    }

    static void MakeArm(BodyPart arm)
    {
        arm.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeHandForSocket(arm.GetSocketsFor(BodyPartType.Hand)[0]);
    }


    public static void MakeHandForSocket(BodyPartSocket socket)
    {
        var hand = socket.TryAttachPart(Defs.BodyParts.OrcHand);
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        hand.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Thumb)[0], Defs.BodyParts.OrcThumb);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[0], Defs.BodyParts.OrcFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[1], Defs.BodyParts.OrcFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[2], Defs.BodyParts.OrcFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[3], Defs.BodyParts.OrcFinger);
        //hand.Equipment[EquipmentSlotType.BuiltIn] = hand.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyHand")!);
    }

    public static void MakeFingerForSocket(BodyPartSocket socket, BodyPartDef def)
    {
        var finger = socket.TryAttachPart(def);
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        finger.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
    }

    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        MakeFootForSocket(leg.GetSocketsFor(BodyPartType.Foot)[0]);
    }

    public static void MakeFootForSocket(BodyPartSocket socket)
    {
        var foot = socket.TryAttachPart(Defs.BodyParts.OrcFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);

        //foot.Equipment[EquipmentSlotType.BuiltIn] = foot.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FleshyFoot")!);
    }
}