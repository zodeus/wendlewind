namespace Wendlewind.Sim.Entities.Pawns.Bodies;

[UsedImplicitly]
public class RustyDollBodyGenerator : IBodyGenerator
{
    public void Generate(Pawn pawn)
    {
        pawn.Body.RootSocket = new BodyPartSocket(Defs.BodyPartSockets.TorsoSocket);
        var core = pawn.Body.RootSocket.TryAttachPart(Defs.BodyParts.RustyDollCore);
        core.Equipment[EquipmentSlotType.BuiltIn] = core.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("RustyDollMouth")!);
        var skull = core.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        var brain = skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        IBodyGenerator.SetSubstanceOverride(pawn, SubstanceType.Metal);
    }

    public static void GenerateMinion(BodyPartSocket minionSocket, double hpMultiplier)
    {
        var minion = minionSocket.TryAttachPart(Defs.BodyParts.RustyDollMinion);
        minion.Equipment[EquipmentSlotType.BuiltIn] = minion.Context.Factory.CreateEntity<Item>(DefRepository<ItemDef>.GetByMoniker("RustyDollMouth")!);
        minion.MaxHitPoints = minion.MaxHitPoints * (hpMultiplier / 2);
        minion.HitPoints = minion.MaxHitPoints;
        var skull = minion.GetSocketsFor(BodyPartType.Skull)[0].TryAttachPart(Defs.BodyParts.Skull);
        var brain = skull.GetSocketsFor(BodyPartType.Brain)[0].TryAttachPart(Defs.BodyParts.Brain);
        skull.SetSubstanceOverride(SubstanceType.Metal);
        brain.SetSubstanceOverride(SubstanceType.Metal);
    }
}
