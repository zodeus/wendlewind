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
    public int Ticks;

    public readonly List<Pawn> PlayerPawns = [];
    public readonly List<Pawn> EnemyPawns = [];
    public readonly CombatRecord CombatRecord = new();
    public readonly List<BodyPart> SeveredLimbs = [];

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

        var playerIsAlive = !PlayerPawns[0].IsDead;
        if (playerIsAlive)
        {
            CollectLoot();
            PlayerPawns[0].Inventory.Trinkets.ForEach(t => t.TrinketHandler?.Stop());
            Core.Context.World.RegisterKill(EnemyPawns[0]);
            if (Def.IsBoss)
            {
                Zone!.IsComplete = true;
            }
        }

        LogMessage($"/f[default, 48]/c[{TC.Golden}]Battle is over\n");
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

            //CollectEquipment(enemy);
        }

        foreach (var part in SeveredLimbs)
        {
            TakePartEquipment(part);
        }

        foreach (var resource in Zone!.BiomeDef.Resources)
        {
            if (Core.Random.Chance(resource.ChanceToHarvest))
            {
                AddToLootContainer(EntityGenerator.CreateEntity<Item>(resource.Item, resource.Amount.RandomValue));
            }
        }

        return;

        void TakePartEquipment(BodyPart part)
        {
            foreach (var (slot, item) in part.Equipment)
            {
                if (item == null || item.ItemDef.EquipmentProperties.SlotUsedToEquip == EquipmentSlotType.BuiltIn) continue;

                part.Equipment[slot] = null;
                AddToLootContainer(item);
            }

            foreach (var externalPart in part.ExternalParts)
            {
                TakePartEquipment(externalPart);
            }
        }
    }

    private void CollectEquipment(Pawn enemy)
    {
        const int chanceToLootEquipment = 1;
        foreach (var (bodyPart, slots) in enemy.Equipment.Slots)
        {
            foreach (var slot in slots.Where(slot => slot is not EquipmentSlotType.BuiltIn))
            {
                if (enemy.Equipment.UnEquip(bodyPart, slot) is { } item && Core.Random.Chance(chanceToLootEquipment))
                {
                    AddToLootContainer(item);
                }
            }
        }
    }

    private void AddToLootContainer(Item item)
    {
        Loot.TryAdd(item);
    }


    public void Tick()
    {
        Ticks++;
        CombatHandler?.DoFighting();
    }
}