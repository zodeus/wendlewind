using System.Collections.Generic;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class World : IExposable {
    public List<Pawn> PlayerPawns = null!;
    public int TotalKills;
    public PawnDeathRecords DeathRecords = null!;

    public void Initialize() {
        PlayerPawns = new List<Pawn>();
        DeathRecords = new PawnDeathRecords();
        TotalKills = 0;
    }

    public void AddPlayerPawn(Pawn pawn) {
        PlayerPawns.Add(pawn);
    }

    public CombatEvent NextCombat() {
        Pawn playerPawn = PlayerPawns[0];
        int nextCombatId = Mathf.Clamp(TotalKills + 1, 1, 15);
        return CombatGenerator.Generate(PlayerPawns, nextCombatId);
    }

    public void ExposeData() { }
}