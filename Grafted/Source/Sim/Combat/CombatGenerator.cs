using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Utils;

namespace Grafted.Sim.Combat;

public class BodyModificationRecord {
    public List<SeverLimbRequest> LimbsToSever = new();
}

public class SeverLimbRequest {
    public BodyPartDef RootLimb = null!;
    public BodyPartSocketDef Socket = null!;
    public bool Seal = true;
}

public static class CombatGenerator {
    public static CombatEvent GenerateIntroCombat(List<Pawn> playerPawns, int combatId) {
        Pawn playerPawn = playerPawns[0];
        CombatEvent combatEvent = new();
        //combatEvent.IsInteractive = true;
        combatEvent.AddPlayerPawn(playerPawn);

        CombatConfigDef combatConfig = DefRepository<CombatConfigDef>.GetByMoniker($"Intro{combatId}")!;
        return Generate(combatConfig, combatEvent);
    }


    public static CombatEvent GenerateForZone(List<Pawn> playerPawns, Zone zone) {
        Pawn playerPawn = playerPawns[0];
        CombatEvent combatEvent = new();
        //combatEvent.IsInteractive = true;
        combatEvent.AddPlayerPawn(playerPawn);


        var zoneConfigs = DefRepository<CombatConfigDef>.Defs.Where(c => c.Moniker.Contains(zone.Def.Moniker)).ToList();
        CombatConfigDef combatConfig = zone.PercentTraveled >= 1 ? zoneConfigs.Last() : zoneConfigs.First(c => zone.DistanceTraveled < c.DistanceToEnd);
        return Generate(combatConfig, combatEvent);
    }

    private static CombatEvent Generate(CombatConfigDef combatConfig, CombatEvent combatEvent) {
        combatEvent.Config = combatConfig;
        var enemies = new List<CombatConfigEnemyRecord> { combatConfig.Enemies.InRandomOrder().First() }; // todo only handling a single enemy
        foreach (CombatConfigEnemyRecord enemyConfig in enemies) {
            Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest(
                enemyConfig.Race,
                enemyConfig.Config
            ));

            pawn.Biography.Name = enemyConfig.PawnName!;
            PawnGenerator.RegisterEquipment(pawn, enemyConfig.EquipmentItems);
            PawnGenerator.RegisterInventory(pawn, enemyConfig.InventoryItems);

            ApplyBodyModifications(pawn, enemyConfig.BodyModifications);

            combatEvent.AddEnemyPawn(pawn);
        }

        return combatEvent;
    }


    private static void ApplyBodyModifications(Pawn pawn, BodyModificationRecord modifications) {
        foreach (SeverLimbRequest severLimbRequest in modifications.LimbsToSever) {
            BodyPart rootPart = pawn.Body.AllExternalParts.First(p => p.BodyPartDef == severLimbRequest.RootLimb);
            BodyPart targetLimb = rootPart.ExternalParts.First(p => p.Socket?.Def == severLimbRequest.Socket);
            BodyPartSocket socket = targetLimb.Socket!;
            targetLimb.Sever();
            socket.IsSealed = severLimbRequest.Seal;
        }
    }
}