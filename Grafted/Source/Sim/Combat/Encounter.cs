using Grafted.Sim.Entities;

namespace Grafted.Sim.Combat;

public class Encounter
{
    private EncounterState _state = EncounterState.NotStarted;
    public CombatHandler? CombatHandler { get; private set; }
    public event Action<EncounterState>? StateChangedAction;

    public Zone? Zone;
    public EncounterDef Def = null!;
    public EntityContainer Loot = new();

    public readonly List<Pawn> PlayerPawns = new();
    public readonly List<Pawn> EnemyPawns = new();
    public readonly CombatRecord CombatRecord = new();
    public readonly List<BodyPart> SeveredLimbs = new();

    public Encounter(Zone zone)
    {
        Zone = zone;
    }

    public void Initialize()
    {
        if (Def.Enemies.Count > 0)
        {
            CombatHandler = new CombatHandler(this);
        }
    }

    public bool AtBoss => Def.IsBoss;
    public bool ShouldAttemptRetreat { get; set; }


    public EncounterState State
    {
        get => _state;
        set
        {
            _state = value;
            StateChangedAction?.Invoke(_state);
        }
    }

    private void OnDeath(DeathEvent deathEvent)
    {
        EndCombat();
    }

    public void AddPlayerPawn(Pawn pawn)
    {
        pawn.Died += OnDeath;
        CombatRecord.AddPawn(pawn);
        PlayerPawns.Add(pawn);
    }

    public void AddEnemyPawn(Pawn pawn)
    {
        pawn.Died += OnDeath;
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
            if (Def.IsBoss)
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
        foreach (var enemy in EnemyPawns)
        {
            for (var i = enemy.Inventory.Count() - 1; i >= 0; i--)
            {
                var item = enemy.Inventory[i];
                if (Core.Context.Player.HasTrinkets(item.ItemDef))
                {
                    continue;
                }

                AddToLootContainer(item);
            }

            /*foreach ((BodyPart? bodyPart, var slots) in enemy.Equipment.Slots)
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
            }*/
        }

        // void TakePartEquipment(BodyPart part)
        // {
        //     foreach ((EquipmentSlotType slot, Item? item) in part.Equipment)
        //     {
        //         if (item != null && item.ItemDef.EquipmentProperties.SlotUsedToEquip != EquipmentSlotType.BuiltIn && Core.Random.Chance(chanceToLootEquipment))
        //         {
        //             part.Equipment[slot] = null;
        //             AddToLootContainer(item);
        //         }
        //     }
        //
        //     foreach (BodyPart externalPart in part.ExternalParts)
        //     {
        //         TakePartEquipment(externalPart);
        //     }
        // }
        //
        // foreach (BodyPart part in SeveredLimbs)
        // {
        //     TakePartEquipment(part);
        // }

        foreach (ZoneResourceRecord resource in Zone!.BiomeDef.Resources)
        {
            if (Core.Random.Chance(resource.ChanceToHarvest))
            {
                AddToLootContainer(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Amount.RandomValue));
            }
        }
    }

    private void AddToLootContainer(Item item)
    {
        Loot.TryAdd(item);
    }


    public void Tick()
    {
        CombatHandler?.DoFighting();
    }
}