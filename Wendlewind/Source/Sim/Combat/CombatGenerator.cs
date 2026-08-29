namespace Wendlewind.Sim.Combat;

public static class CombatGenerator
{
    public static Encounter GenerateForZone(Pawn playerPawn, Zone zone)
    {
        var encounterDef = zone.ZoneDef.Encounters[zone.Stage];

        // Select a random weather from zone's available weathers
        WeatherDef? weather = zone.ZoneDef.Weathers.Count > 0
            ? zone.ZoneDef.Weathers.RandomElement()
            : null;

        Encounter encounter = new(zone, encounterDef, weather);
        encounter.AddPlayerPawn(playerPawn);

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