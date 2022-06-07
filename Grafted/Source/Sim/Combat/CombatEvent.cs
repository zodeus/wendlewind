using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Sim.Zones;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;

namespace Grafted.Sim.Combat;

public class CombatEvent {
    private CombatState _state = CombatState.Preparation;
    private readonly Dictionary<Pawn, Item> _queuedPotions = new();
    private readonly List<CombatBuff> _buffs = new();

    public event Action<CombatState>? StateChangedAction;

    public Zone? Zone;
    public CombatConfigDef Config = null!;
    public CombatTurn CurrentTurn = null!;
    public int CurrentTurnNum;
    public bool IsInteractive = false;
    public EntityContainer Loot = new();

    public readonly List<Pawn> PlayerPawns = new();
    public readonly List<Pawn> EnemyPawns = new();
    public readonly CombatRecord CombatRecord = new();
    public readonly List<BodyPart> SeveredLimbs = new();
    public readonly List<CombatTurn> Turns = new();
    public bool IsBoss { get; set; }
    public bool ShouldAttemptRetreat { get; set; }
    public bool Retreated { get; set; }
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
        if (IsBoss) {
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.LightGoldenrodYellow, Duration = 5,
                Font = BaseContent.Fonts.Fancy.Huge,
                Text = "BOSS FIGHT!"
            });
        }

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
            Core.Sim.Messages.Push(
                new Message(
                    $"\\c[{UiTextColor.TextColorPawn}]{PlayerPawns.First().Label} \\c[{UiTextColor.TextColorDefault}]ran away from \\c[{UiTextColor.TextColorEnemyPawn}]{EnemyPawns.First().Label}",
                    Color.Green
                )
            );
            return;
        }

        bool playerIsAlive = !PlayerPawns[0].IsDead;
        if (playerIsAlive) {
            CollectLoot();
            Core.Sim.World.RegisterKill(EnemyPawns[0]);
            if (Config.IsBoss) {
                Zone!.BossKilledThisRun = true;
                Zone!.IsComplete = true;
            }
        }

        LogMessage("Battle is over");
    }

    public void LogMessage(string message) {
        CombatRecord.LogMessage(new CombatLogMessage {
            Text = message,
        });
    }

    public bool AttemptRetreat() {
        ShouldAttemptRetreat = false;
        if (Core.Random.Chance(0.5f)) {
            CombatRecord.Retreated = true;
            CombatRecord.LogMessage(new CombatLogMessage { Text = "\\c[YellowGreen]Retreated successfully" });
            State = CombatState.CombatEnd;
            Retreated = true;
            return true;
        }

        CombatRecord.LogMessage(new CombatLogMessage { Text = "\\c[#acc700]Failed to retreat" });
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

    private void CollectLoot() {
        float chanceToLootEquipment = 0.07f;
        foreach (Pawn enemy in EnemyPawns) {
            for (int i = enemy.Inventory.Count() - 1; i >= 0; i--) {
                Item item = enemy.Inventory.Entities[i];
                if (Core.Sim.Player.HasTrinket(item.ItemDef)) {
                    continue;
                }

                AddToLootContainer(item);
            }

            foreach ((BodyPart? bodyPart, var slots) in enemy.Equipment.Slots) {
                foreach (EquipmentSlotType slot in slots) {
                    if (slot is EquipmentSlotType.BuiltIn) {
                        continue;
                    }

                    if (enemy.Equipment.UnEquip(bodyPart, slot) is { } item && Core.Random.Chance(chanceToLootEquipment)) {
                        AddToLootContainer(item);
                    }
                }
            }
        }

        void TakePartEquipment(BodyPart part) {
            foreach ((EquipmentSlotType slot, Item? item) in part.Equipment) {
                if (item != null && item.ItemDef.EquipmentProperties.SlotUsedToEquip != EquipmentSlotType.BuiltIn && Core.Random.Chance(chanceToLootEquipment)) {
                    part.Equipment[slot] = null;
                    AddToLootContainer(item);
                }
            }

            foreach (BodyPart externalPart in part.ExternalParts) {
                TakePartEquipment(externalPart);
            }
        }

        foreach (BodyPart part in SeveredLimbs) {
            TakePartEquipment(part);
        }

        if (Zone != null) {
            foreach (ZoneResourceRecord resource in Zone.Def.Resources.Where(r => r.HarvestArea.Includes(Zone.PercentTraveledThisRun))) {
                if (Core.Random.Chance(resource.ChanceToHarvest)) {
                    AddToLootContainer(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Amount.RandomValue));
                }
            }
        }
    }

    private void AddToLootContainer(Item item) {
        if (item.ItemDef.ItemType == ItemType.Trinket) {
            Core.Sim.Player.TrinketsFound.Add(item.ItemDef);
        }

        Loot.TryAdd(item);
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

    public void Tick() {
        foreach (Pawn pawn in EnemyPawns) {
            pawn.Tick();
        }
    }
}