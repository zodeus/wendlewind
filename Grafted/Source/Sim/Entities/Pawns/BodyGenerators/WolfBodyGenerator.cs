using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns.BodyGenerators;

public static class WolfBodyGenerator {
    public static void Generate(Pawn pawn) {
        pawn.Body.RootSocket = GenerateBody();
        pawn.Body.BodyPartsDirty = true; //todo this should be set by/in BodyPart, but BodyPart doesn't have access to Pawn currently
        GenerateBuiltInTools(pawn);
    }

    private static BodyPartSocket GenerateBody() {
        BodyPartSocket rootSocket = new(Defs.BodyPartSockets.HeadSocket);
        BodyPart head = rootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.WolfHead));
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        // Skull
        BodyPart skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        // Neck
        BodyPart neck = head.GetSocketsFor(BodyPartType.Neck)[0].TryAttachPart(Defs.BodyParts.WolfNeck);
        neck.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        neck.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        neck.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);

        // Torso
        BodyPart torso = neck.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.WolfTorso);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);

        // RibCage
        BodyPart ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.RibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.WolfLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.WolfLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[2].TryAttachPart(Defs.BodyParts.WolfLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[3].TryAttachPart(Defs.BodyParts.WolfLeg));
        
        // Tail
        torso.GetSocketsFor(BodyPartType.Tail)[0].TryAttachPart(Defs.BodyParts.WolfTail);
        
        return rootSocket;
    }

    static void MakeLeg(BodyPart leg) {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        BodyPart foot = leg.GetSocketsFor(BodyPartType.Paw)[0].TryAttachPart(Defs.BodyParts.WolfPaw);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
    }

    private static void GenerateBuiltInTools(Pawn pawn) {
        Item teeth = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("WolfTeeth")!);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Head && p.SlotFor(teeth) != null).ToList()[0], teeth);
        
        
        Item claw0 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("WolfClaws")!);
        Item claw1 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("WolfClaws")!);
        Item claw2 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("WolfClaws")!);
        Item claw3 = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("WolfClaws")!);
        
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Paw && p.SlotFor(claw0) != null).ToList()[0], claw0);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Paw && p.SlotFor(claw0) != null).ToList()[1], claw1);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Paw && p.SlotFor(claw0) != null).ToList()[2], claw2);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.Where(p => p.Type == BodyPartType.Paw && p.SlotFor(claw0) != null).ToList()[3], claw3);
    }
}