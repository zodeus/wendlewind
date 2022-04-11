using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grafted.Maths;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;

namespace Grafted.Sim.Combat;

public class CombatEvent {
    private CombatState _state = CombatState.Preparation;
    public bool IsInteractive = false;

    public CombatState State {
        get { return _state; }
        set {
            _state = value;
            StateChangedAction?.Invoke(_state);
        }
    }

    public event Action<CombatState>? StateChangedAction;

    public List<Pawn> PlayerPawns = new();
    public List<Pawn> EnemyPawns = new();
    public List<CombatTurn> Turns = new();
    public CombatTurn CurrentTurn = null!;
    public int CurrentTurnNum;
    public CombatRecord CombatRecord = new();

    public void AddPlayerPawn(Pawn pawn) {
        CombatRecord.AddPawn(pawn);
        PlayerPawns.Add(pawn);
    }

    public void AddEnemyPawn(Pawn pawn) {
        CombatRecord.AddPawn(pawn);
        EnemyPawns.Add(pawn);
    }

    public void StartAsCoroutine() {
        Core.StartCoroutine(TurnEngine());
    }

    public void Execute() {
        IEnumerator engine = TurnEngine();
        while (engine.MoveNext()) {
            object? current = engine.Current;
        }
    }

    private IEnumerator TurnEngine() {
        //DeepProfiler.enabled = false;
        while (State != CombatState.CombatEnd) {
            CurrentTurnNum++;
            State = CombatState.TurnStart;
            CurrentTurn = new CombatTurn(this);
            Turns.Add(CurrentTurn);

            if (PlayerPawns.Any(p => p.IsDead == false) == false || EnemyPawns.Any(p => p.IsDead == false) == false) {
                State = CombatState.CombatEnd;
                break;
            }

            CurrentTurn.RegisterPawns(PlayerPawns);
            CurrentTurn.RegisterPawns(EnemyPawns);
            CurrentTurn.Prepare();
            IEnumerator handler = CurrentTurn.Run();
            while (handler.MoveNext()) {
                yield return handler.Current;
            }

            if (State == CombatState.CombatEnd) {
                break;
            }

            State = CombatState.TurnEnd;
        }

        Log.Debug("Exiting turn engine");
        EndCombat();
        State = CombatState.CombatFinished;
    }

    private void EndCombat() {
        State = CombatState.CombatEnd;
        //todo Core.Sim.World.CombatEvents.Record(CombatRecord);
        if (CombatRecord.Retreated) {
            Core.Sim.Messages.Push(new Message($"\\c[{CombatSequence.TextColorPawn}]{PlayerPawns.First().Label} \\c[{CombatSequence.TextColorDefault}]ran away from \\c[{CombatSequence.TextColorEnemyPawn}]{EnemyPawns.First().Label}",
                Color.Green));
            return;
        }

        bool playerPawnDied = CombatRecord.Pawns.First(p => p.Faction == "Player").WasKilled;
        if (playerPawnDied) {
            Core.Sim.Messages.Push(new Message($"\\c[{CombatSequence.TextColorPawn}]{PlayerPawns.First().Label} \\c[{CombatSequence.TextColorRed}] was killed by \\c[{CombatSequence.TextColorEnemyPawn}]{EnemyPawns.First().Label}"));
        }
        else {
            Core.Sim.World.TotalKills++;
            Core.Sim.Messages.Push(new Message($"\\c[{CombatSequence.TextColorPawn}]{PlayerPawns.First().Label} \\c[{CombatSequence.TextColorGreen}]killed \\c[{CombatSequence.TextColorEnemyPawn}]{EnemyPawns.First().Label}"));
        }
        LogMessage($"Battle is over");
    }

    public void LogMessage(string message) {
        CombatRecord.LogMessage(new CombatLogMessage {
            Text = message,
        });
    }

    public bool AttemptRetreat() {
        if (Core.Random.Chance(0.5f)) {
            CombatRecord.Retreated = true;
            CombatRecord.LogMessage(new CombatLogMessage { Text = "\\c[YellowGreen]Retreated successfully" });
            State = CombatState.CombatEnd;
            return true;
        }

        CombatRecord.LogMessage(new CombatLogMessage { Text = "\\c[#acc700]Failed to retreat" });
        return false;
    }

    public bool ShouldAttemptRetreat(CombatTurn combatTurn, Pawn pawn) {
        Pawn target = combatTurn.Pawns.First(target => target.PawnDef.PawnType != pawn.PawnDef.PawnType);
        if (pawn.Brain.CombatSettings.IsAutoRetreatEnabledFor(target.Race)) {
            return true;
        }

        if (combatTurn.PawnTurnData[pawn].WantsToRetreat) {
            return true;
        }

        return false;
    }

    public bool IsPartyDead(Pawn member) {
        if (PlayerPawns.Any(p => p.PawnDef.PawnType == member.PawnDef.PawnType && p.IsDead == false)) {
            return false;
        }

        if (EnemyPawns.Any(p => p.PawnDef.PawnType == member.PawnDef.PawnType && p.IsDead == false)) {
            return false;
        }

        return true;
    }
}