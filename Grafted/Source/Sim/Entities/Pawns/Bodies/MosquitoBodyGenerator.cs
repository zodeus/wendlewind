namespace Grafted.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class MosquitoBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.HeadSocket);
        var head = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.MosquitoHead));
        head.GetSocketsFor(BodyPartType.Eye)[0].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Eye)[1].TryAttachPart(Defs.BodyParts.Eye);
        head.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        head.GetSocketsFor(BodyPartType.Antenna)[0].TryAttachPart(Defs.BodyParts.MosquitoAntenna);
        head.GetSocketsFor(BodyPartType.Antenna)[1].TryAttachPart(Defs.BodyParts.MosquitoAntenna);
        var proboscis = head.GetSocketsFor(BodyPartType.Proboscis)[0].TryAttachPart(Defs.BodyParts.MosquitoProboscis);
        proboscis.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("FlamingMosquitoProboscis")!);

        // Thorax
        var thorax = head.GetSocketsFor(BodyPartType.Thorax)[0].TryAttachPart(Defs.BodyParts.MosquitoThorax);
        thorax.GetSocketsFor(BodyPartType.Heart)[0].TryAttachPart(Defs.BodyParts.Heart);
        thorax.GetSocketsFor(BodyPartType.Stomach)[0].TryAttachPart(Defs.BodyParts.Stomach);
        thorax.GetSocketsFor(BodyPartType.Leg)[0].TryAttachPart(Defs.BodyParts.MosquitoLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[1].TryAttachPart(Defs.BodyParts.MosquitoLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[2].TryAttachPart(Defs.BodyParts.MosquitoLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[3].TryAttachPart(Defs.BodyParts.MosquitoLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[4].TryAttachPart(Defs.BodyParts.MosquitoLeg);
        thorax.GetSocketsFor(BodyPartType.Leg)[5].TryAttachPart(Defs.BodyParts.MosquitoLeg);

        // Abdomen
        var abdomen = thorax.GetSocketsFor(BodyPartType.Abdomen)[0].TryAttachPart(Defs.BodyParts.MosquitoAbdomen);
        abdomen.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("PoisonousProboscis")!);
        abdomen.GetSocketsFor(BodyPartType.Wing)[0].TryAttachPart(Defs.BodyParts.MosquitoWing);
        abdomen.GetSocketsFor(BodyPartType.Wing)[1].TryAttachPart(Defs.BodyParts.MosquitoWing);
    }
}