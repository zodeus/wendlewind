namespace Grafted.Sim.Entities.Pawns.Bodies;

public class GameBoxBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.GameBoxHead));
        head.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GameBoxScreen")!);

        // Torso (connected directly to head since no neck)
        var torso = head.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.GameBoxTorso);
        var skull = torso.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        
        // Brain is the only organic part - housed inside the metal torso
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Cpu);

        // Misc
        var controls =torso.GetSocketsFor(BodyPartType.Misc)[0].TryAttachPart(Defs.BodyParts.GameBoxControls);
        controls.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GameBoxDefense")!);

        // Arms
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[0].TryAttachPart(Defs.BodyParts.GameBoxArm));
        MakeArm(torso.GetSocketsFor(BodyPartType.Arm)[1].TryAttachPart(Defs.BodyParts.GameBoxArm));

        // Legs (no feet - legs are the terminal parts)
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.GameBoxLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.GameBoxLeg));
    }

    static void MakeArm(BodyPart arm)
    {
        var hand = arm.GetSocketsFor(BodyPartType.Hand)[0].TryAttachPart(Defs.BodyParts.GameBoxHand);
        hand.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GameBoxFist")!);
    }

    static void MakeLeg(BodyPart leg)
    {
        leg.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("GameBoxFoot")!);
    }
}
