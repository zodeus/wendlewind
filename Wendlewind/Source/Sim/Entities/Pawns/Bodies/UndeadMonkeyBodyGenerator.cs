namespace Wendlewind.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class UndeadMonkeyBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.UndeadMonkeyHead));
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        head.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("UndeadMonkeyTeeth")!);

        // Skull
        var skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Torso (connected directly to head - no visible neck)
        var torso = head.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.UndeadMonkeyTorso);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);
        torso.GetSocketsFor(BodyPartType.Liver)[0].TryAttachPart(Defs.BodyParts.Liver);
        torso.GetSocketsFor(BodyPartType.Kidney)[0].TryAttachPart(Defs.BodyParts.Kidney);
        torso.GetSocketsFor(BodyPartType.Kidney)[1].TryAttachPart(Defs.BodyParts.Kidney);
        torso.GetSocketsFor(BodyPartType.Spleen)[0].TryAttachPart(Defs.BodyParts.Spleen);
        torso.GetSocketsFor(BodyPartType.Intestines)[0].TryAttachPart(Defs.BodyParts.Intestines);

        // RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.RibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);

        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.UndeadMonkeyArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.UndeadMonkeyArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.UndeadMonkeyLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.UndeadMonkeyLeg));

        // Tail
        var tail = torso.GetSocketsFor(BodyPartType.Tail)[0].TryAttachPart(Defs.BodyParts.UndeadMonkeyTail);
        tail.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        tail.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        tail.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
    }

    private static void MakeArm(BodyPart arm)
    {
        arm.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeHand(arm.GetSocketsFor(BodyPartType.Hand)[0]);
    }

    private static void MakeHand(BodyPartSocket socket)
    {
        var hand = socket.TryAttachPart(Defs.BodyParts.UndeadMonkeyHand);
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        hand.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Thumb)[0], Defs.BodyParts.UndeadMonkeyThumb);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[0], Defs.BodyParts.UndeadMonkeyFinger);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[1], Defs.BodyParts.UndeadMonkeyFinger);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[2], Defs.BodyParts.UndeadMonkeyFinger);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[3], Defs.BodyParts.UndeadMonkeyFinger);
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("UndeadMonkeyClaws")!);
    }

    private static void MakeFinger(BodyPartSocket socket, BodyPartDef def)
    {
        var finger = socket.TryAttachPart(def);
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        finger.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
    }

    private static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        MakeFoot(leg.GetSocketsFor(BodyPartType.Foot)[0]);
    }

    private static void MakeFoot(BodyPartSocket socket)
    {
        var foot = socket.TryAttachPart(Defs.BodyParts.UndeadMonkeyFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        foot.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("UndeadMonkeyKick")!);
    }
}
