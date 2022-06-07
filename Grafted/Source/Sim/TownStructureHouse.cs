using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Persistence;
using JetBrains.Annotations;

namespace Grafted.Sim;

public class AlchemyBarrel : IExposable {
    private readonly TownStructureHouse _house;
    private int _ticks;

    public ItemDef? ItemDef;
    public Item? Item;

    public AlchemyBarrel(TownStructureHouse house) {
        _house = house;

    }

    public int TimeLeft => ItemDef == null ? 0 : ItemDef.CraftingProperties.MinutesToMake - _ticks;

    public void SetItemToCraft(ItemDef item) {
        _ticks = 0;
        ItemDef = item;
    }

    public void Tick() {
        Item?.Tick();
        if (ItemDef == null) {
            return;
        }

        _ticks++;
        if (_ticks >= ItemDef.CraftingProperties.MinutesToMake) {
            Item = EntityGenerator.CreateEntity<Item>(ItemDef, ItemDef.CraftingProperties.AmountProduced);
            ItemDef = null;
        }
    }

    public void TransferToStorage() {
        Core.Sim.Messages.Push(new Message($"Bottling barrel contents, adding to house storage"));
        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(30));
        _house.Storage.TryAdd(Item);
        Item = null;
    }

    public void ExposeData() {
        Scribe_Values.Look(ref _ticks, "Ticks");
        Scribe_Defs.Look(ref ItemDef!, "ItemDef");
        Scribe_Deep.Look(ref Item!, "Item");
    }
}

[UsedImplicitly]
public class TownStructureHouse : TownStructure, IExposable {
    private int _burningLogTicks = 0;

    public int Firewood;
    public bool IsFireBurning;
    public bool HasMeatRack;
    public bool HasAlchemyBarrel;
    public EntityContainer Storage = null!;
    public Dictionary<int, Item?> MeatRack = null!;
    public Dictionary<int, int> MeatTicks = null!;
    public AlchemyBarrel Barrel = null!;

    public override void Tick() {
        Barrel.Tick();
        Storage.Tick();
        if (!IsFireBurning) {
            return;
        }

        foreach ((int slot, Item? item) in MeatRack) {
            if (item is { ItemDef: { FoodProperties: { FoodType: FoodType.RawMeat } } }) {
                MeatTicks[slot]++;
                if (MeatTicks[slot] > SimTime.HoursToTicks(16)) {
                    MeatTicks[slot] = 0;
                    MeatRack[slot]!.Destroy();
                    MeatRack[slot] = EntityGenerator.CreateEntity<Item>(Defs.Items.DriedMeat, 1);
                }
            }
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
            $"\\c[{UiTextColor.TextColorDefault}] for \\c[{UiTextColor.TextColorGreen}]{woodLog.StackSize * 100} \\c[{UiTextColor.TextColorItem}]firewood"
        ));
        for (int i = 0; i < woodLog.StackSize; i++) {
            Core.Sim.PlayerPawn.Body.ConsumeEnergy(0.25f);
            Core.Sim.World.ProgressTime(SimTime.HoursToSeconds(2));
            Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.Firewood, 100));
        }

        woodLog.StackSize = 0;
        woodLog.Destroy();
    }

    public override void Initialize() {
        Storage = new();
        MeatRack = new() {
            { 0, null },
            { 1, null },
        };
        MeatTicks = new() {
            { 0, 0 },
            { 1, 0 }
        };

        Barrel = new AlchemyBarrel(this);
        Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.Firewood, 100));
        //Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.RawMeat, 20));
        //Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.CookedMeat, 10));
    }

    public void CutBoard(Item woodLog) {
        Core.Sim.Messages.Push(new Message(
            $"Cut \\c[{UiTextColor.TextColorGreen}]{woodLog.StackSize} \\c[{UiTextColor.TextColorItem}]wood log" +
            $"\\c[{UiTextColor.TextColorDefault}] for \\c[{UiTextColor.TextColorGreen}]{woodLog.StackSize * 8} \\c[{UiTextColor.TextColorItem}]wood boards"
        ));
        for (int i = 0; i < woodLog.StackSize; i++) {
            Core.Sim.PlayerPawn.Body.ConsumeEnergy(0.30f);
            Core.Sim.World.ProgressTime(SimTime.SecondsInHour * 4);
            Storage.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.WoodBoard, 8));
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
        Core.Sim.World.PlayerPawn.IsResting = true;
        Core.Sim.World.ProgressUntilTimeOfDay(Core.Random.Next(5, 9) * 100 + Core.Random.Next(0, 59));
        Core.Sim.World.PlayerPawn.IsResting = false;
        Core.Sim.World.PlayerPawn.Body.Energy = 1;
    }

    public void CraftItem(ItemDef item, int amount) {
        Core.Sim.Messages.Push(new Message($"Cooking \\c[{UiTextColor.TextColorGreen}]{amount}x \\c[{UiTextColor.TextColorItem}]{item.Label}"));
        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(item.CraftingProperties.MinutesToMake));
        foreach (ResourceCount resourceCount in item.CraftingProperties.ResourceRequirements) {
            Core.Sim.Player.TakeItem(resourceCount.Resource!, resourceCount.Count * amount)!.Destroy();
        }

        Storage.TryAdd(EntityGenerator.CreateEntity<Item>(item, amount * item.CraftingProperties.AmountProduced));
    }

    public bool HasRequirementsFor(ItemDef item, int amountWanted) {
        if (IsFireBurning == false) {
            return false;
        }

        foreach (ResourceCount resourceCount in item.CraftingProperties.ResourceRequirements) {
            int amount = resourceCount.Count * amountWanted;
            if (Core.Sim.Player.AmountOfItem(resourceCount.Resource) < amount) {
                return false;
            }
        }

        if (item.CraftingProperties.RequiredTools != null) {
            foreach (ItemDef requiredTool in item.CraftingProperties.RequiredTools) {
                if (Core.Sim.Player.AmountOfItem(requiredTool) < 1) {
                    return false;
                }
            }
        }

        return true;
    }

    public void TryStartBrewing(ItemDef item) {
        Core.Sim.Messages.Push(new Message($"Adding ingredients to \\c[{UiTextColor.TextColorItem}]alchemy barrel \\c[{UiTextColor.TextColorDefault}]to begin brewing \\c[{UiTextColor.TextColorItem}]{item.Label}"));
        Core.Sim.World.ProgressTime(SimTime.MinutesToSeconds(15));
        foreach (ResourceCount resourceCount in item.CraftingProperties.ResourceRequirements) {
            Core.Sim.Player.TakeItem(resourceCount.Resource!, resourceCount.Count)!.Destroy();
        }

        Barrel.SetItemToCraft(item);
    }

    public void AddMeatToDryingRack(Item meat, int slot) {
        if (meat.StackSize > 1) {
            Log.Warning($"Adding item to meat rack with stack size > 1, {meat} stack size={meat.StackSize}");
        }

        MeatRack[slot] = meat;
    }

    public override void ExposeData() {
        Scribe_Values.Look(ref _burningLogTicks, "BurningLogTicks");
        Scribe_Values.Look(ref Firewood, "Firewood");
        Scribe_Values.Look(ref IsFireBurning, "IsFireBurning");
        Scribe_Values.Look(ref HasMeatRack, "HasMeatRack");
        Scribe_Values.Look(ref HasAlchemyBarrel, "HasAlchemyBarrel");
        Scribe_Deep.Look(ref Barrel!, "Barrel", this);
        Scribe_Deep.Look(ref Storage!, "Storage");
        Scribe_Collections.Look(ref MeatRack!, "MeatRack", LookMode.Value, LookMode.Deep);
        Scribe_Collections.Look(ref MeatTicks!, "MeatTicks", LookMode.Value, LookMode.Value);
        base.ExposeData();
    }
}