namespace Grafted.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class FrogBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
       pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.FrogHead));
        head.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        head.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("BlisteringToadTongue")!);

        //Skull
        var skull = head.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);

        //Torso
        var torso = head.GetSocketsFor(BodyPartType.Torso)[0].TryAttachPart(Defs.BodyParts.FrogTorso);
        torso.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        torso.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);

        //RibCage
        var ribCage = torso.GetSocketsFor(BodyPartType.RibCage)[0].TryAttachPart(Defs.BodyParts.RibCage);
        ribCage.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        ribCage.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        ribCage.GetSocketsFor(BodyPartType.Lung)[0].TryAttachPart(Defs.BodyParts.Lung);
        ribCage.GetSocketsFor(BodyPartType.Lung)[1].TryAttachPart(Defs.BodyParts.Lung);

        // Legs
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.FrogLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.FrogLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[2].TryAttachPart(Defs.BodyParts.FrogLeg));
        MakeLeg(torso.GetSocketsFor(BodyPartType.Leg)[3].TryAttachPart(Defs.BodyParts.FrogLeg));
    }
    
    static void MakeLeg(BodyPart leg)
    {
        leg.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        leg.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
        leg.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        var foot = leg.GetSocketsFor(BodyPartType.Foot)[0].TryAttachPart(Defs.BodyParts.FrogFoot);
        foot.GetSocketsFor(BodyPartType.Artery)[0].TryAttachPart(Defs.BodyParts.Artery);
        foot.GetSocketsFor(BodyPartType.Skin)[0].TryAttachPart(Defs.BodyParts.Skin);
        foot.GetSocketsFor(BodyPartType.Bone)[0].TryAttachPart(Defs.BodyParts.Bone);
    }
}