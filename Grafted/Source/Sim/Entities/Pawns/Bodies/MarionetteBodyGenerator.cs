namespace Grafted.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class MarionetteBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.MarionetteHead));
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("MarionetteTeeth")!);

        // Torso (connected directly to head - no neck)
        var torso = head.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.MarionetteTorso);

        // Heart - the puppet's magical core
        torso.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);

        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.MarionetteArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.MarionetteArm));

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.MarionetteLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.MarionetteLeg));

        IBodyGenerator.SetSubstanceOverride(pawn, SubstanceType.Wood);
    }

    static void MakeArm(BodyPart arm)
    {
        MakeHand(arm.GetSocketsFor(BodyPartType.Hand)[0].TryAttachPart(Defs.BodyParts.MarionetteHand));
    }

    static void MakeHand(BodyPart hand)
    {
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("MarionetteClaws")!);
    }

    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.MarionetteFoot);
    }
}
