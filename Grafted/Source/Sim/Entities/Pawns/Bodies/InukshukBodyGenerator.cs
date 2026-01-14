namespace Grafted.Sim.Entities.Pawns.Bodies;

/// <summary>
/// Body generator for Inukshuk pawns.
/// Inukshuks are stone creatures with a simplified structure:
/// Head -> Torso -> Arms (2) and Legs (2).
/// No separate hands/feet, as the limbs are solid stone.
/// </summary>
[UsedImplicitly]
public class InukshukBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.InukshukHead));

        // Skull (stone core)
        var skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Torso
        var torso = head.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.InukshukTorso);

        // Arms (simple stone limbs - no hands, arms ARE the weapons)
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.InukshukArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.InukshukArm));

        // Legs (simple stone limbs - no feet, legs ARE the weapons)
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.InukshukLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.InukshukLeg));

        IBodyGenerator.SetSubstanceOverride(pawn, SubstanceType.Stone);
    }

    static void MakeArm(BodyPart arm)
    {
        arm.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        arm.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("InukshukStoneFist")!);
    }

    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("InukshukStoneLeg")!);
    }
}

