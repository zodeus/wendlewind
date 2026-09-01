﻿namespace Wendlemire.Sim.Entities.Pawns.Bodies;

public class TreebornBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.TreeTrunkSocket);
        GenerateBodyInSocket(pawn.Body.RootSocket);
        IBodyGenerator.SetSubstanceOverride(pawn, SubstanceType.Wood);
    }

    private static void GenerateBodyInSocket(BodyPartSocket rootSocket)
    {
        var torso = rootSocket.TryAttachPart(Defs.BodyParts.TreeTrunk);
        torso.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        torso.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);

        //RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.TreeInnerCore);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Legs
        MakeStump(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.TreeLegStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.TreeLegStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Leg)[2].TryAttachPart(Defs.BodyParts.TreeLegStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Leg)[3].TryAttachPart(Defs.BodyParts.TreeLegStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.TreeArmStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.TreeArmStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Arm)[2].TryAttachPart(Defs.BodyParts.TreeArmStump));
        MakeStump(torso.GetSocketsFor(BodyPartType.Arm)[3].TryAttachPart(Defs.BodyParts.TreeArmStump));
    }


    static void MakeStump(BodyPart stump)
    {
        stump.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        stump.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        stump.Equipment[EquipmentSlotType.BuiltIn] = stump.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("TreeBranch")!);
    }
}