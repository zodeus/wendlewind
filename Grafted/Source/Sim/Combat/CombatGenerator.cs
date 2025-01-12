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
        var encounterDef = DefRepository<EncounterDef>.Defs
            .Where(d => d.Biome == zone.BiomeDef)
            .Take(new Range(zone.Stage, zone.Stage + 1))
            .First();
        encounter.Def = encounterDef;

        if (encounter.Def.Enemies.Count != 0)
        {
            GenerateEnemies(encounter);
        }

        encounter.Initialize();

        return encounter;
    }

    private static void GenerateEnemies(Encounter encounter)
    {
        // todo only handling a single enemy
        var enemies = new List<EncounterEnemyRecord>
        {
            encounter.Def.Enemies.RandomElementByWeight(c => c.SpawnWeight)!
        };

        foreach (var enemyConfig in enemies)
        {
            var pawn = PawnGenerator.CreatePawn(new PawnRequest(
                enemyConfig.Race,
                enemyConfig.Config
            )
            {
                BodySizeFactor = enemyConfig.BodySizeFactor
            });

            pawn.Biography.Name = enemyConfig.PawnName;
            PawnGenerator.RegisterEquipment(pawn, enemyConfig.EquipmentItems);
            PawnGenerator.RegisterInventory(pawn, enemyConfig.InventoryItems);
            PawnGenerator.RegisterSkills(pawn, enemyConfig.Skills);

            ApplyBodyModifications(pawn, enemyConfig.BodyModifications);
            ApplyEffects(pawn, enemyConfig.Effects);

            encounter.AddEnemyPawn(pawn);
        }
    }

    private static void ApplyEffects(Pawn pawn, List<BodyEffectDef> effects)
    {
        foreach (var effect in effects)
        {
            pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = effect, TicksLeft = 99999
            });
        }
    }

    private static void ApplyBodyModifications(Pawn pawn, BodyModificationRecord modifications)
    {
        foreach (var severLimbRequest in modifications.LimbsToSever)
        {
            var rootPart = pawn.Body.AllExternalParts.First(p => p.BodyPartDef == severLimbRequest.RootLimb);
            var targetLimb = rootPart.ExternalParts.First(p => p.Socket?.Def == severLimbRequest.Socket);
            var socket = targetLimb.Socket!;
            targetLimb.Severe();
            socket.IsSealed = severLimbRequest.Seal;
        }
    }
}