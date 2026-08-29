namespace Wendlewind.Sim.Combat;

public static class CombatGenerator
{
    public static Encounter GenerateForZone(GameContext context, Pawn playerPawn, Zone zone, int? seed = null)
    {
        int encounterSeed = seed ?? SeedUtility.EncounterSeed(
            context.RunSeed,
            zone.ZoneDef.Moniker,
            zone.Stage);

        context.Rng = new Random(encounterSeed);

        var encounterDef = zone.ZoneDef.Encounters[zone.Stage];

        WeatherDef? weather = zone.ZoneDef.Weathers.Count > 0
            ? zone.ZoneDef.Weathers.RandomElement(context.Rng)
            : null;

        Encounter encounter = new(zone, encounterDef, weather) { Seed = encounterSeed, Context = context };
        encounter.AddPlayerPawn(playerPawn);

        if (encounter.Def.Enemies.Count != 0)
        {
            GenerateEnemies(encounter);
        }

        encounter.Initialize();

        return encounter;
    }

    public static Encounter GenerateHumanDuel(GameContext context, Pawn playerPawn, Pawn enemyPawn, Zone zone, int? seed = null)
    {
        int encounterSeed = seed ?? SeedUtility.EncounterSeed(
            context.RunSeed,
            zone.ZoneDef.Moniker,
            zone.Stage);

        context.Rng = new Random(encounterSeed);

        Encounter encounter = new(zone, new EncounterProperties(), weather: null)
        {
            Seed = encounterSeed,
            Context = context
        };
        encounter.AddPlayerPawn(playerPawn);
        encounter.AddEnemyPawn(enemyPawn);
        encounter.Initialize();
        return encounter;
    }

    private static void GenerateEnemies(Encounter encounter)
    {
        // todo only handling a single enemy
        var enemies = new List<EncounterEnemyRecord>
        {
            encounter.Def.Enemies.RandomElementByWeight(c => c.SpawnWeight, encounter.Context.Rng)!
        };

        foreach (var enemyConfig in enemies)
        {
            var pawn = PawnGenerator.CreatePawn(encounter.Context, new PawnRequest(
                enemyConfig.PawnName,
                enemyConfig.PawnDef,
                enemyConfig.Loadout,
                PawnType.Enemy,
                enemyConfig.BodySizeFactor
            ));

            PawnGenerator.RegisterEquipment(pawn, enemyConfig.EquipmentItems);
            PawnGenerator.RegisterInventory(pawn, enemyConfig.InventoryItems);
            PawnGenerator.RegisterSkills(pawn, enemyConfig.Skills);

            ApplyBodyModifications(pawn, enemyConfig.BodyModifications);
            ApplyEffects(pawn, enemyConfig.Effects);

            // Ensure enemies spawn with full stomachs
            pawn.Body.StomachLevel = 1;

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