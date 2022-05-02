using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
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
        combatEvent.Zone = zone;
        //combatEvent.IsInteractive = true;
        combatEvent.AddPlayerPawn(playerPawn);
        CombatConfigDef combatConfig = DefRepository<CombatConfigDef>.Defs.Where(CombatFilter(zone)).RandomElement();
        return Generate(combatConfig, combatEvent);
    }

    private static Func<CombatConfigDef, bool> CombatFilter(Zone zone) {
        return config => {
            if (config.Zone != zone.Def) {
                return false;
            }

            if (config.SpawnRange.Includes(zone.PercentTraveled) == false) {
                return false;
            }

            if (config.Enemies.Any(record => Core.Sim.World.Time.IsTimeOfDay(record.SpawnDuring)) == false) {
                return false;
            }

            return true;
        };
    }

    private static CombatEvent Generate(CombatConfigDef combatConfig, CombatEvent combatEvent) {
        combatEvent.Config = combatConfig;
        var enemies = new List<CombatConfigEnemyRecord> {
            combatConfig.Enemies.Where(record => Core.Sim.World.Time.IsTimeOfDay(record.SpawnDuring))
                .RandomElementByWeight(c => c.SpawnWeight)!
        }; // todo only handling a single enemy
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