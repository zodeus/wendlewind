namespace Grafted.Sim.Combat;

public class BodyModificationRecord
{
    public List<SevereLimbRequest> LimbsToSever = new();
}

public class SevereLimbRequest
{
    public BodyPartDef RootLimb = null!;
    public BodyPartSocketDef Socket = null!;
    public bool Seal = true;
}

public static class CombatGenerator
{
    public static Encounter GenerateForZone(Pawn playerPawn, Zone zone)
    {
        Encounter encounter = new(zone);
        encounter.AddPlayerPawn(playerPawn);
        CombatConfigDef combatConfig;
        combatConfig = DefRepository<CombatConfigDef>.Defs
            .Where(d => d.Biome == zone.BiomeDef)
            .Take(new Range(zone.ZoneKills, zone.ZoneKills + 1))
            .First();
        if (combatConfig.IsBoss)
        {
            encounter.AtBoss = true;
        }

        Generate(combatConfig, encounter);
        encounter.Initialize();
        return encounter;
    }

    private static Encounter Generate(CombatConfigDef combatConfig, Encounter encounter)
    {
        encounter.Config = combatConfig;
        var enemies = new List<CombatConfigEnemyRecord>
        {
            combatConfig.Enemies.RandomElementByWeight(c => c.SpawnWeight)!
        }; // todo only handling a single enemy
        foreach (CombatConfigEnemyRecord enemyConfig in enemies)
        {
            Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest(
                enemyConfig.Race,
                enemyConfig.Config
            )
            {
                BodySizeFactor = enemyConfig.BodySizeFactor
            });

            pawn.Biography.Name = enemyConfig.PawnName!;
            PawnGenerator.RegisterEquipment(pawn, enemyConfig.EquipmentItems);
            PawnGenerator.RegisterInventory(pawn, enemyConfig.InventoryItems);
            PawnGenerator.RegisterSkills(pawn, enemyConfig.Skills);

            ApplyBodyModifications(pawn, enemyConfig.BodyModifications);
            ApplyEffects(pawn, enemyConfig.Effects);

            encounter.AddEnemyPawn(pawn);
        }

        return encounter;
    }

    private static void ApplyEffects(Pawn pawn, List<BodyEffectDef> effects)
    {
        foreach (BodyEffectDef effect in effects)
        {
            pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = effect, TicksLeft = 200
            });
        }
    }


    private static void ApplyBodyModifications(Pawn pawn, BodyModificationRecord modifications)
    {
        foreach (SevereLimbRequest severLimbRequest in modifications.LimbsToSever)
        {
            BodyPart rootPart = pawn.Body.AllExternalParts.First(p => p.BodyPartDef == severLimbRequest.RootLimb);
            BodyPart targetLimb = rootPart.ExternalParts.First(p => p.Socket?.Def == severLimbRequest.Socket);
            BodyPartSocket socket = targetLimb.Socket!;
            targetLimb.Severe();
            socket.IsSealed = severLimbRequest.Seal;
        }
    }
}