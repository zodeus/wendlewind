using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;

namespace Grafted.Sim.Combat;

public class CombatEvent {
    public event Action<CombatState>? StateChangedAction;

    public List<Pawn> PlayerPawns = new();
    public List<Pawn> EnemyPawns = new();
    public List<CombatTurn> Turns = new();
    public CombatTurn CurrentTurn = null!;
    public int CurrentTurnNum;
    public CombatRecord CombatRecord = new();
    public List<BodyPart> SeveredLimbs = new();
    public bool IsInteractive = false;

    private CombatState _state = CombatState.Preparation;
    private Dictionary<Pawn, Item> _queuedPotions = new();
    private List<CombatBuff> _buffs = new();

    public IReadOnlyList<CombatBuff> Buffs => _buffs;

    public CombatState State {
        get => _state;
        set {
            _state = value;
            StateChangedAction?.Invoke(_state);
        }
    }

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

        bool playerPawnDied = PlayerPawns[0].IsDead;
        if (playerPawnDied) {
            Core.Sim.Messages.Push(new Message($"\\c[{CombatSequence.TextColorPawn}]{PlayerPawns.First().Label} \\c[{CombatSequence.TextColorRed}] was killed by \\c[{CombatSequence.TextColorEnemyPawn}]{EnemyPawns.First().Label}"));
        }
        else {
            Core.Sim.World.TotalKills++;
            Core.Sim.Messages.Push(new Message($"\\c[{CombatSequence.TextColorPawn}]{PlayerPawns.First().Label} \\c[{CombatSequence.TextColorGreen}]killed \\c[{CombatSequence.TextColorEnemyPawn}]{EnemyPawns.First().Label}"));
            UpdateWorldStuff();
        }

        LogMessage("Battle is over");
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
        Pawn target = combatTurn.Pawns.First(target => target.PawnType != pawn.PawnType);
        if (pawn.Brain.CombatSettings.IsAutoRetreatEnabledFor(target.Race)) {
            return true;
        }

        if (combatTurn.PawnTurnData[pawn].WantsToRetreat) {
            return true;
        }

        return false;
    }

    public bool IsPartyDead(Pawn member) {
        if (PlayerPawns.Any(p => p.PawnType == member.PawnType && p.IsDead == false)) {
            return false;
        }

        if (EnemyPawns.Any(p => p.PawnType == member.PawnType && p.IsDead == false)) {
            return false;
        }

        return true;
    }

    private void UpdateWorldStuff() {
        Pawn playerPawn = PlayerPawns[0];
        playerPawn.Body.BloodAmount = playerPawn.Body.MaxBlood;


        void AddToInventory(Item? i) {
            if (i == null) { return; }

            if (playerPawn.Inventory.Count(it => it.Def == i.Def) < 2) {
                playerPawn.Inventory.Items.TryAdd(i);
            }
        }

        foreach (Pawn enemy in EnemyPawns) {
            for (int i = enemy.Inventory.Count() - 1; i >= 0; i--) {
                Item item = enemy.Inventory.Items[i];
                enemy.Inventory.Items.Remove(item);
                playerPawn.Inventory.Items.TryAdd(item);
            }

            foreach ((BodyPart? bodyPart, var slots) in enemy.Equipment.Slots) {
                foreach (EquipmentSlotType slot in slots) {
                    if (slot is EquipmentSlotType.BuiltIn) {
                        continue;
                    }

                    AddToInventory(enemy.Equipment.UnEquip(bodyPart, slot));
                }
            }
        }

        void TakePartEquipment(BodyPart part) {
            foreach ((EquipmentSlotType slot, Item? item) in part.Equipment) {
                if (item != null && item.ItemDef.EquipmentProperties.SlotUsedToEquip != EquipmentSlotType.BuiltIn) {
                    part.Equipment[slot] = null;
                    AddToInventory(item);
                }
            }

            foreach (BodyPart externalPart in part.ExternalParts) {
                TakePartEquipment(externalPart);
            }
        }

        foreach (BodyPart part in SeveredLimbs) {
            TakePartEquipment(part);
        }

        if (Core.Sim.World.TotalKills == 10) {
            Core.Sim.GameSpeed = .2f; //todo THIS IS JUNK
        }

        foreach (BodyPart part in playerPawn.Body.AllParts) {
            if (part.HealthPercent >= .97) { continue; }

            if (part.Type == BodyPartType.Skin) {
                part.HitPoints += Mathf.FloorToInt(part.MaxHitPoints * Core.Random.NextFloat(0.10f, 0.25f));
                continue;
            }

            if (part.IsDestroyed) {
                /*if (Core.Random.Chance(.04f)) {
                    part.HitPoints = 1;
                }*/

                continue;
            }

            part.HitPoints += Mathf.FloorToInt(part.MaxHitPoints * Core.Random.NextFloat(0.03f, 0.08f));
        }
    }

    public void QueuePotion(Item potion, Pawn pawn) {
        _queuedPotions[pawn] = potion;
    }

    public Item? DeQueuedPotionFor(Pawn pawn) {
        if (_queuedPotions.ContainsKey(pawn)) {
            Item potion = _queuedPotions[pawn];
            _queuedPotions.Remove(pawn);
            return potion;
        }

        return null;

    }

    public Item? PotionQueuedFor(Pawn pawn) {
        return _queuedPotions.ContainsKey(pawn) ? _queuedPotions[pawn] : null;

    }

    public bool HasBuff(Pawn pawn, ItemDef buff) {
        foreach (CombatBuff combatBuff in _buffs) {
            if (combatBuff.Pawn == pawn && buff == combatBuff.Def) {
                return true;
            }
        }

        return false;
    }

    public void ActivateBuff(Item item, Pawn pawn, int duration) {
        _buffs.Add(new CombatBuff(item.Def, pawn, duration));
    }

    public void RemoveBuffsFor(Pawn pawn) {
        for (int i = _buffs.Count - 1; i >= 0; i--) {
            if (_buffs[i].Pawn == pawn) {
                _buffs.RemoveAt(i);
            }
        }
    }

    public void RemoveBuff(CombatBuff buff) {
        _buffs.Remove(buff);
    }
}

public class CombatBuff {
    public readonly EntityDef Def;
    public readonly Pawn Pawn;
    public int Duration;

    public CombatBuff(EntityDef def, Pawn pawn, int duration) {
        Def = def;
        Pawn = pawn;
        Duration = duration;
    }
}