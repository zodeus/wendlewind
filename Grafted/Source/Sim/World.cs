using System.Collections.Generic;
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

    public void ExposeData() { }
}