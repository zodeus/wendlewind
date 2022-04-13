using System;
using System.Collections.Generic;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Combat;

public class CombatRecord : IExposable {
    private List<CombatLogMessage> _logs = new();
    public IReadOnlyList<CombatLogMessage> Logs => _logs;
    public List<PawnCombatRecord> Pawns = new();
    public bool Retreated = false;
    public event Action<CombatLogMessage>? LogMessageAddedAction;


    public void AddPawn(Pawn pawn) {
        Pawns.Add(new PawnCombatRecord {
            Id = pawn.Id,
            Faction = pawn.PawnType.ToString(),
            Name = pawn.Label,
            WasKilled = false
        });
    }

    public void ExposeData() { }

    public void LogMessage(CombatLogMessage message) {
        _logs.Add(message);
        LogMessageAddedAction?.Invoke(message);
    }
}

public class CombatLogMessage : IExposable {
    public string Text;

    public void ExposeData() { }
}

public class PawnCombatRecord : IExposable {
    public int Id;
    public string Name;
    public string Faction;
    public bool WasKilled;

    public override string ToString() {
        return $"{Name} ({Faction})";
    }

    public void ExposeData() { }
}