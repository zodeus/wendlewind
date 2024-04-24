using Grafted.Sim.Entities;

namespace Grafted.Sim.Combat;

public class Encounter
{
    private EncounterState _state = EncounterState.NotStarted;
    private readonly Dictionary<Pawn, Item> _queuedPotions = new();
    private readonly List<CombatBuff> _buffs = new();
    private CombatHandler CombatHandler = null!;
    public event Action<EncounterState>? StateChangedAction;

    public Zone? Zone;
    public CombatConfigDef Config = null!;
    public EntityContainer Loot = new();

    public readonly List<Pawn> PlayerPawns = new();
    public readonly List<Pawn> EnemyPawns = new();
    public readonly CombatRecord CombatRecord = new();
    public readonly List<BodyPart> SeveredLimbs = new();
    private string? _deathMessage;

    public Encounter(Zone zone)
    {
        Zone = zone;
    }

    public void Initialize()
    {
        CombatHandler = new CombatHandler(this);
    }

    public bool AtBoss { get; set; }
    public bool ShouldAttemptRetreat { get; set; }
    public IReadOnlyList<CombatBuff> Buffs => _buffs;


    public EncounterState State
    {
        get => _state;
        set
        {
            _state = value;
            StateChangedAction?.Invoke(_state);
        }
    }

    private void LogDeathMessage(Pawn pawn, string causeOfDeath)
    {
        _deathMessage = $"\\c[{TC.Victim}]{pawn.LabelShort} \\c[{TC.Red}]died from {causeOfDeath}";
    }

    public void AddPlayerPawn(Pawn pawn)
    {
        pawn.OnDeath += LogDeathMessage;
        CombatRecord.AddPawn(pawn);
        PlayerPawns.Add(pawn);
    }

    public void AddEnemyPawn(Pawn pawn)
    {
        pawn.OnDeath += LogDeathMessage;
        CombatRecord.AddPawn(pawn);
        EnemyPawns.Add(pawn);
    }

    private void EndCombat()
    {
        State = EncounterState.Finished;

        bool playerIsAlive = !PlayerPawns[0].IsDead;
        if (playerIsAlive)
        {
            CollectLoot();
            Core.Context.World.RegisterKill(EnemyPawns[0]);
            if (Config.IsBoss)
            {
                Zone!.IsComplete = true;
            }
        }

        LogMessage("Battle is over");
    }

    public void LogMessage(string message)
    {
        CombatRecord.LogMessage(new CombatLogMessage
        {
            Text = message,
        });
    }

    private void CollectLoot()
    {
        float chanceToLootEquipment = 1;
        foreach (Pawn enemy in EnemyPawns)
        {
            for (int i = enemy.Inventory.Count() - 1; i >= 0; i--)
            {
                Item item = enemy.Inventory.Entities[i];
                if (Core.Context.Player.HasTrinket(item.ItemDef))
                {
                    continue;
                }

                AddToLootContainer(item);
            }

            foreach ((BodyPart? bodyPart, var slots) in enemy.Equipment.Slots)
            {
                foreach (EquipmentSlotType slot in slots)
                {
                    if (slot is EquipmentSlotType.BuiltIn)
                    {
                        continue;
                    }

                    if (enemy.Equipment.UnEquip(bodyPart, slot) is { } item && Core.Random.Chance(chanceToLootEquipment))
                    {
                        AddToLootContainer(item);
                    }
                }
            }
        }

        void TakePartEquipment(BodyPart part)
        {
            foreach ((EquipmentSlotType slot, Item? item) in part.Equipment)
            {
                if (item != null && item.ItemDef.EquipmentProperties.SlotUsedToEquip != EquipmentSlotType.BuiltIn && Core.Random.Chance(chanceToLootEquipment))
                {
                    part.Equipment[slot] = null;
                    AddToLootContainer(item);
                }
            }

            foreach (BodyPart externalPart in part.ExternalParts)
            {
                TakePartEquipment(externalPart);
            }
        }

        foreach (BodyPart part in SeveredLimbs)
        {
            TakePartEquipment(part);
        }

        // foreach (ZoneResourceRecord resource in Zone.BiomeDef.Resources)
        // {
        //     if (Core.Random.Chance(resource.ChanceToHarvest))
        //     {
        //         AddToLootContainer(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Amount.RandomValue));
        //     }
        // }
    }

    private void AddToLootContainer(Item item)
    {
        if (item.ItemDef.ItemType == ItemType.Trinket)
        {
            Core.Context.Player.TrinketsFound.Add(item.ItemDef);
        }

        Loot.TryAdd(item);
    }

    public void QueuePotion(Item potion, Pawn pawn)
    {
        _queuedPotions[pawn] = potion;
    }

    public Item? DeQueuedPotionFor(Pawn pawn)
    {
        if (_queuedPotions.ContainsKey(pawn))
        {
            Item potion = _queuedPotions[pawn];
            _queuedPotions.Remove(pawn);
            return potion;
        }

        return null;
    }

    public Item? PotionQueuedFor(Pawn pawn)
    {
        return _queuedPotions.ContainsKey(pawn) ? _queuedPotions[pawn] : null;
    }

    public bool HasBuff(Pawn pawn, ItemDef buff)
    {
        foreach (CombatBuff combatBuff in _buffs)
        {
            if (combatBuff.Pawn == pawn && buff == combatBuff.Def)
            {
                return true;
            }
        }

        return false;
    }

    public void ActivateBuff(Item item, Pawn pawn, int duration)
    {
        _buffs.Add(new CombatBuff(item.Def, pawn, duration));
    }

    public void RemoveBuffsFor(Pawn pawn)
    {
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            if (_buffs[i].Pawn == pawn)
            {
                _buffs.RemoveAt(i);
            }
        }
    }

    public void RemoveBuff(CombatBuff buff)
    {
        _buffs.Remove(buff);
    }

    public void Tick(int ticks)
    {
        CombatHandler.DoFighting(ticks);
        foreach (var pawn in EnemyPawns)
        {
            pawn.Tick(ticks);
            if (pawn.IsDead)
            {
                RemoveBuffsFor(pawn);
            }
            else
            {
                // float bloodLost = PawnTurnData[pawn].StartingBloodLevel - pawn.Body.BloodAmount;
                // if (bloodLost > 0) {
                //     _combatEvent.LogMessage($"\\c[{UiTextColor.TextColorPawn}]{pawn.LabelShort} is losing blood \\c[{UiTextColor.TextColorRed}]-{bloodLost:0.00}");
                // }
            }
        }

        //
        // for (int index = _combatEvent.Buffs.Count - 1; index >= 0; index--) {
        //     CombatBuff buff = _combatEvent.Buffs[index];
        //     buff.Duration--;
        //     if (buff.Duration <= 0) {
        //         _combatEvent.RemoveBuff(buff);
        //         if (buff.Def == Defs.Items.PumpinJuice) {
        //             _combatEvent.LogMessage($"\\c[{UiTextColor.TextColorPawn}]{buff.Pawn.LabelShort} feels heavy from pump drain");
        //             //_combatEvent.ActivateBuff(Heavy, );
        //         }
        //     }
        // }
        if (_deathMessage != null)
        {
            LogMessage(_deathMessage);
            EndCombat();
        }
    }
}