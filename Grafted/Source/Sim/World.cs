using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class World : IExposable {
    public List<Pawn> PlayerPawns = null!;
    public int TotalKills;

    public void Initialize() {
        PlayerPawns = new List<Pawn>();
        TotalKills = 0;
    }

    public void AddPlayerPawn(Pawn pawn) {
        PlayerPawns.Add(pawn);
    }

    public CombatEvent NextCombat() {
        Pawn playerPawn = PlayerPawns[0];
        CombatEvent combatEvent = new();
        //combatEvent.IsInteractive = true;
        combatEvent.AddPlayerPawn(playerPawn);
        Pawn pawn = PawnGenerator.CreatePawn(new PawnRequest(
            DefRepository<RaceDef>.GetByMoniker("Caucasian")!,
            Defs.PawnConfigs.TheHelplessMan
        ));
        ItemDef weaponDef;
        if (Core.Sim.World.TotalKills > 8) {
            weaponDef = DefRepository<ItemDef>.GetByMoniker("Mace")!;
        }
        else {
            weaponDef = DefRepository<ItemDef>.GetByMoniker("WoodenStick")!;
        }

        Item weapon = EntityGenerator.CreateEntity<Item>(weaponDef, 1);
        pawn.Equipment.TryEquip(pawn.Body.AllParts.First(p => p.SlotFor(weapon) != null), weapon);

        combatEvent.AddEnemyPawn(pawn);
        return combatEvent;
    }

    public void ExposeData() { }
}