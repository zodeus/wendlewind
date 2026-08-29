namespace Wendlewind.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class RustyDollBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.TorsoSocket);
        var core = pawn.Body.RootSocket.TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.RustyDollCore));
        core.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("RustyDollMouth")!);
        var skull = core.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        var brain = skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        IBodyGenerator.SetSubstanceOverride(pawn, SubstanceType.Metal);
    }

    public static void GenerateMinion(BodyPartSocket minionSocket, double hpMultiplier)
    {
        var minion = EntityGenerator.CreateEntity<BodyPart>(Defs.BodyParts.RustyDollMinion);
        minionSocket.TryAttachPart(minion);
        minion.Equipment[EquipmentSlotType.BuiltIn] = EntityGenerator.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("RustyDollMouth")!);
        minion.MaxHitPoints = minion.MaxHitPoints * (hpMultiplier / 2);
        minion.HitPoints = minion.MaxHitPoints;
        var skull = minion.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        var brain = skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        skull.SetSubstanceOverride(SubstanceType.Metal);
        brain.SetSubstanceOverride(SubstanceType.Metal);
    }
}
