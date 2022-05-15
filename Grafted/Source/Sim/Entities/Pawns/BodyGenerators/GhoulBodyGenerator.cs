using Grafted.Definitions;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns.BodyGenerators;

public static class GhoulBodyGenerator {
    public static void MakeHandForSocket(BodyPartSocket socket) {
        BodyPart hand = socket.TryAttachPart(Defs.BodyParts.GhoulHand);
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Thumb)[0], Defs.BodyParts.GhoulThumb);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[0], Defs.BodyParts.GhoulFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[1], Defs.BodyParts.GhoulFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[2], Defs.BodyParts.GhoulFinger);
        MakeFingerForSocket(hand.GetSocketsFor(BodyPartType.Finger)[3], Defs.BodyParts.GhoulFinger);
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GhoulishHand")!);
    }

    public static void MakeFingerForSocket(BodyPartSocket socket, BodyPartDef def) {
        BodyPart finger = socket.TryAttachPart(def);
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
    }
}