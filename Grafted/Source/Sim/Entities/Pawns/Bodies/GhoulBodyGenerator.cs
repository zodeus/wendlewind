namespace Grafted.Sim.Entities.Pawns.Bodies;

public class GhoulBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.GhoulHead));
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        //Skull
        var skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Neck
        var neck = head.GetSocketsFor(BodyPartType.Neck)[0].TryAttachPart(Defs.BodyParts.GhoulNeck);
        neck.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        neck.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        //Torso
        var torso = neck.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.GhoulTorso);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);

        //RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.RibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].IsSealed = true;
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);


        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.GhoulArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.GhoulArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.GhoulLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.GhoulLeg));
    }

    static void MakeArm(BodyPart arm)
    {
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeHandForSocket(arm.GetSocketsFor(BodyPartType.Hand)[0]);
    }

    public static void MakeHandForSocket(BodyPartSocket socket)
    {
        var hand = socket.TryAttachPart(Defs.BodyParts.GhoulHand);
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Thumb)[0], Defs.BodyParts.GhoulThumb);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[0], Defs.BodyParts.GhoulFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[1], Defs.BodyParts.GhoulFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[2], Defs.BodyParts.GhoulFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[3], Defs.BodyParts.GhoulFinger);
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GhoulishHand")!);
    }

    public static void MakeFingerForSocket(BodyPartSocket socket, BodyPartDef def)
    {
        var finger = socket.TryAttachPart(def);
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
    }

    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        var foot = leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.GhoulFoot);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);

        foot.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GhoulishFoot")!);
    }
}