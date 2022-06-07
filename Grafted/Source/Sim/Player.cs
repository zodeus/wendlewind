using System;
using System.Collections.Generic;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class Player : IExposable {
    private Pawn _pawn = null!;

    public List<ItemDef> TrinketsFound = null!;

    public TownStructureHouse House = null!;
    public EntityContainer Storage => House.Storage;
    public string Label => "You"; //Pawn.Label;
    public Pawn Pawn => _pawn;

    public int AmountOfItem(ItemDef? item) {
        int amount = Pawn.Inventory.Entities.AmountOf(item);
        amount += Storage.AmountOf(item);
        return amount;
    }

    public void Initialize(Pawn pawn) {
        _pawn = pawn;
        TrinketsFound = new List<ItemDef>();
    }

    public void ResetPawn(Pawn pawn) {
        _pawn.Destroy();
        _pawn = pawn;
    }

    public Item? TakeItem(ItemDef itemToTake, int amount) {
        //todo don't take from storage if not at home
        if (AmountOfItem(itemToTake) < amount) {
            Log.Warning($"House requirement failed, wanted {amount}x of {itemToTake.Label} but only had {AmountOfItem(itemToTake)}x");
        }

        Item? item = Pawn.Inventory.Entities.Take(itemToTake, amount);
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


    public void Tick() {
        Pawn.Tick();
    }

    public void ExposeData() {
        Scribe_Deep.Look(ref _pawn!, "Pawn");
        Scribe_Collections.Look(ref TrinketsFound!, "TrinketsFound", LookMode.Def);
        Scribe_References.Look(ref House!, "House");
    }

    public bool HasTrinket(ItemDef def) {
        return TrinketsFound.Contains(def);
    }

    public IEnumerable<Entity> FindItems(Func<Item, bool> filter) {
        foreach (Item item in _pawn.Inventory) {
            if (filter(item)) {
                yield return item;
            }
        }

        foreach (Item item in Storage) {
            if (filter(item)) {
                yield return item;
            }
        }
    }
}