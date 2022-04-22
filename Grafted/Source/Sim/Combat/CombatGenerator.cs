using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public class CombatConfigDef : Def {
    public List<CombatConfigEnemyRecord> Enemies = new();
}

public class CombatConfigEnemyRecord {
    public RaceDef Race = null!;
    public PawnConfigDef Config = null!;
    public string PawnName = null;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public BodyModificationRecord BodyModifications = new();
}

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

        CombatConfigDef combatConfig = DefRepository<CombatConfigDef>.GetByMoniker($"{zone.Def.Moniker}-01")!;
        return Generate(combatConfig, combatEvent);
    }

    private static CombatEvent Generate(CombatConfigDef combatConfig, CombatEvent combatEvent) {
        foreach (CombatConfigEnemyRecord enemyConfig in combatConfig.Enemies) {
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