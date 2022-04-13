using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Combat;

public class CombatConfigDef : Def {
    public List<CombatConfigEnemyRecord> Enemies;
}

public class CombatConfigEnemyRecord {
    public RaceDef Race = null!;
    public PawnConfigDef Config = null!;
    public string PawnName = null;
    public List<ItemDef> EquipmentItems = new();
    public List<ItemDropCount> InventoryItems = new();
    public List<BodyModificationRecord> BodyModifications = new();
}

public class BodyModificationRecord {
    public BodyPartDef RootLimb = null!;
    public BodyPartSocketDef Socket = null!;
    public bool Seal = true;
}

public static class CombatGenerator {
    public static CombatEvent Generate(List<Pawn> playerPawns) {
        int nextCombatId = Mathf.Clamp(Core.Sim.World.TotalKills + 1, 1, 10);
        Pawn playerPawn = playerPawns[0];
        CombatEvent combatEvent = new();
        //combatEvent.IsInteractive = true;
        CombatConfigDef combatConfig = DefRepository<CombatConfigDef>.GetByMoniker($"Combat{nextCombatId}")!;
        foreach (CombatConfigEnemyRecord enemyConfig in combatConfig.Enemies) {
            combatEvent.AddPlayerPawn(playerPawn);
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

    private static void ApplyBodyModifications(Pawn pawn, List<BodyModificationRecord> modifications) {
        foreach (BodyModificationRecord modification in modifications) {
            BodyPart rootPart = pawn.Body.AllExternalParts.First(p => p.BodyPartDef == modification.RootLimb);
            BodyPart targetLimb = rootPart.ExternalParts.First(p => p.Socket?.Def == modification.Socket);
            BodyPartSocket socket = targetLimb.Socket!;
            targetLimb.Severe();
            socket.IsSealed = modification.Seal;
        }
    }

    private static void ApplyBodyModifications() { }
}