using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using JetBrains.Annotations;

namespace Grafted.Sim;

public class Town {
    public Dictionary<TownStructureDef, TownStructure> _structures = new();
    public ZoneDef ZoneDef = null!;

    public void Tick() {
        foreach (TownStructure structure in _structures.Values) {
            structure.Tick();
        }
    }

    public void AddStructure(TownStructure structure) {
        _structures.Add(structure.Def, structure);
    }

    public T? GetStructure<T>() where T : TownStructure {
        foreach (TownStructure? structure in _structures.Values) {
            if (structure is T item) {
                return item;
            }
        }

        return null;
    }
}

public class TownStructureDef : Def {
    [UsedImplicitly] public Type StructureClass = typeof(TownStructure);
}

public abstract class TownStructure {
    public TownStructureDef Def = null!;
    public int Id = -1;
    public Town Town = null!;

    public virtual void Tick() { }

    public virtual void Initialize() { }
}

[UsedImplicitly]
public class TownStructureHouse : TownStructure {
    private int _burningLogTicks = 0;

    public ItemContainer Storage = new();
    public int Firewood;
    public bool IsFireBurning;

    public override void Tick() {
        Storage.Tick();
        if (!IsFireBurning) {
            return;
        }

        _burningLogTicks++;
        if (_burningLogTicks < SimTime.HoursToTicks(1)) {
            return;
        }

        _burningLogTicks = 0;
        Firewood--;
        if (Firewood <= 0) {
            Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorRed}]Fire went out"));
            IsFireBurning = false;
        }
    }

    public void EnterHouse(Pawn pawn) { }

    public void ExitHouse(Pawn pawn) { }

    public void StartFire() {
        Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorGreen}]Started a fire"));
        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(15));
        if (Firewood <= 0) {
            return;
        }

        IsFireBurning = true;
    }

    public void ChopFirewood(Item woodLog) {
        Core.Sim.Messages.Push(new Message(
            $"Chopped \\c[{UiTextColor.TextColorGreen}]{woodLog.StackSize} \\c[{UiTextColor.TextColorItem}]wood log" +
            $"\\c[{UiTextColor.TextColorDefault}] for \\c[{UiTextColor.TextColorGreen}]{woodLog.StackSize * 20} \\c[{UiTextColor.TextColorItem}]firewood"
        ));
        for (int i = 0; i < woodLog.StackSize; i++) {
            Core.Sim.World.PlayerPawns[0].Body.ApplyEnergyLoss(0.15f);
            Core.Sim.World.ProgressTime(SimTime.SecondsInHour);
            Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.Firewood, 20));
        }

        woodLog.StackSize = 0;
        woodLog.Destroy();
    }

    public void AddWoodToFire(Item wood) {
        Core.Sim.Messages.Push(new Message(
            $"Added \\c[{UiTextColor.TextColorGreen}]{wood.StackSize} \\c[{UiTextColor.TextColorItem}]firewood \\c[{UiTextColor.TextColorDefault}]to \\c[{UiTextColor.TextColorItem}]fireplace"
        ));
        Core.Sim.World.ProgressTime(30 + 30 * wood.StackSize);
        Firewood += wood.StackSize;
        wood.StackSize = 0;
        wood.Destroy();

    }

    public void Rest() {
        Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorBlue}]Sleeping zzz...zz..z"));
        Core.Sim.World.ProgressTimeUntil(Core.Random.Next(5, 9) * 100 + Core.Random.Next(0, 59));
        Core.Sim.World.PlayerPawn.Body.Energy = 1;
    }

    public void CraftItem(ItemDef item, int amount) {
        Core.Sim.Messages.Push(new Message($"Cooking \\c[{UiTextColor.TextColorGreen}]{amount}x \\c[{UiTextColor.TextColorItem}]{item.Label}"));
        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(item.CraftingProperties.MinutesToMake));
        foreach (ResourceCount resourceCount in item.CraftingProperties.ResourceRequirements) {
            TakeItem(resourceCount.Resource!, resourceCount.Count * amount)!.Destroy();
        }

        Storage.TryAdd(EntityGenerator.CreateEntity<Item>(item, amount));
    }

    public bool HasRequirementsFor(ItemDef item, int amountWanted) {
        if (IsFireBurning == false) {
            return false;
        }

        foreach (ResourceCount resourceCount in item.CraftingProperties.ResourceRequirements) {
            int amount = resourceCount.Count * amountWanted;
            if (AmountOfItem(resourceCount.Resource) < amount) {
                return false;
            }
        }

        return true;
    }

    public int AmountOfItem(ItemDef? resource) {
        int amount = Core.Sim.World.PlayerPawn.Inventory.Items.AmountOf(resource);
        amount += Storage.AmountOf(resource);
        return amount;
    }

    public Item? TakeItem(ItemDef itemToTake, int amount) {
        if (AmountOfItem(itemToTake) < amount) {
            Log.Warning($"House requirement failed, wanted {amount}x of {itemToTake.Label} but only had {AmountOfItem(itemToTake)}x");
        }

        Item? item = Core.Sim.World.PlayerPawn.Inventory.Items.Take(itemToTake, amount);
        if (item?.StackSize >= amount) {
            return item;
        }

        amount -= item?.StackSize ?? 0;
        Item? storageItem = Storage.Take(itemToTake, amount);
        if (item != null) {
            item.StackSize += storageItem?.StackSize ?? 0;
            storageItem?.Destroy();
            return item;
        }

        return storageItem;
    }
}

[UsedImplicitly]
public class TownStructureMerchant : TownStructure {
    private int _lastRefreshTick = 0;
    private int _refreshInterval = SimTime.HoursToTicks(24);
    public ItemContainer Items = null!;

    public override void Initialize() {
        Items = new ItemContainer(9999);
    }

    public override void Tick() {
        if (_lastRefreshTick == 0 || _lastRefreshTick + _refreshInterval <= Core.Sim.Ticks) {
            _lastRefreshTick = Core.Sim.Ticks;
            TownGenerator.PopulateMerchantContainer(this);
        }

        Items.Tick();
    }
}