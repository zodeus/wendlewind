namespace Grafted.Sim.Entities.Pawns.Bodies;

public class MushroomBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        // Torso
        pawn.Body.RootSocket = new(Defs.BodyPartSockets.TorsoSocket);
        var stump = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.MushroomStump));

        // Cap
        var cap = stump.GetSocketsFor(BodyPartType.Head)[0].TryAttachPart(Defs.BodyParts.MushroomCap);
        cap.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("MushroomCapWeapon")!);

        // Eyes
        stump.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        stump.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);

        //Skull
        var skull = stump.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Arms
        MakeArm(stump.GetSocketsFor(BodyPartType.Arm)[0]);
        MakeArm(stump.GetSocketsFor(BodyPartType.Arm)[1]);

        // Legs
        MakeLeg(stump.GetSocketsFor(BodyPartType.Leg)[0]);
        MakeLeg(stump.GetSocketsFor(BodyPartType.Leg)[1]);

        IBodyGenerator.SetSubstanceOverride(pawn, SubstanceType.Fungus);
    }

    static void MakeArm(BodyPartSocket socket)
    {
        var arm = socket.TryAttachPart(Defs.BodyParts.MushroomArm);
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeHandForSocket(arm.GetSocketsFor(BodyPartType.Hand)[0]);
    }

    public static void MakeHandForSocket(BodyPartSocket socket)
    {
        var hand = socket.TryAttachPart(Defs.BodyParts.MushroomHand);
        hand.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
    }

    static void MakeLeg(BodyPartSocket socket)
    {
        var leg = socket.TryAttachPart(Defs.BodyParts.MushroomLeg);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        MakeFootForSocket(leg.GetSocketsFor(BodyPartType.Foot)[0]);
    }

    public static void MakeFootForSocket(BodyPartSocket socket)
    {
        var foot = socket.TryAttachPart(Defs.BodyParts.MushroomFoot);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
    }
}