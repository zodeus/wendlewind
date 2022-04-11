using Grafted.Definitions;

namespace Grafted.Sim.Entities.Pawns;

public static class PawnBodiesTmp {
    public static BodyPartSocket GenerateHuman() {
        BodyPartSocket rootSocket = new(Defs.BodyPartSockets.HeadSocket);
        BodyPart head = rootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.HumanHead));
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.HumanEye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.HumanEye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);

        //Skull
        BodyPart skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.HumanSkull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.HumanBrain);

        // Neck
        BodyPart neck = head.GetSocketsFor(BodyPartType.Neck)[0].TryAttachPart(Defs.BodyParts.HumanNeck);
        neck.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        neck.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        neck.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);

        //Torso
        BodyPart torso = neck.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.HumanTorso);
        torso.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.HumanStomach);

        //RibCage
        BodyPart ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.HumanRibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.HumanHeart);


        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.HumanArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.HumanArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.HumanLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.HumanLeg));

        return rootSocket;
    }

    static void MakeArm(BodyPart arm) {
        arm.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        arm.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        MakeHand(arm.GetSocketsFor(BodyPartType.Hand)[0].TryAttachPart(Defs.BodyParts.HumanHand));
    }

    static void MakeHand(BodyPart hand) {
        var artery = hand.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        //artery.HitPoints = 0;
        hand.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        MakeFinger(hand.GetSocketsFor(BodyPartType.Thumb)[0].TryAttachPart(Defs.BodyParts.HumanThumb));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[0].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[1].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[2].TryAttachPart(Defs.BodyParts.HumanFinger));
        MakeFinger(hand.GetSocketsFor(BodyPartType.Finger)[3].TryAttachPart(Defs.BodyParts.HumanFinger));
    }

    static void MakeFinger(BodyPart finger) {
        finger.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        finger.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        finger.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
    }

    static void MakeLeg(BodyPart leg) {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        BodyPart foot = leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.HumanFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.HumanArtery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.HumanSkin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.HumanBone);
    }
}