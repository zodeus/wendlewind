using System.Collections;
using System.Collections.Generic;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui;
using Microsoft.Xna.Framework;

namespace Grafted.Sim.Entities;

public interface IEntityContainer : IEnumerable<Entity> { }

public class ItemContainer : IEntityContainer, IEnumerable<Item> {
    private readonly List<Item> _list;
    private int _weight;
    private int _maxWeight;

    public ItemContainer(int maxWeight = 999) {
        _list = new List<Item>();
        _maxWeight = maxWeight;
    }

    public int Weight => _weight;
    public int MaxWeight => _maxWeight;

    public bool HasCapacityFor(Item item, int? count = null) {
        return count == null ? item.Weight + Weight <= MaxWeight : (item.WeightSingle * count) + Weight <= MaxWeight;
    }

    public void Tick() {
        for (int index = _list.Count - 1; index >= 0; index--) {
            Entity entity = _list[index];
            entity.Tick();
            if (entity.IsDestroyed) {
                _list.RemoveAt(index);
                CalculateWeight();
            }
        }
    }

    private void CalculateWeight() {
        _weight = 0;
        foreach (Item item in _list) {
            _weight += item.Weight;
        }
    }

    public Item this[int i] => _list[i];

    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() {
        return GetEnumerator();
    }

    public IEnumerator<Item> GetEnumerator() {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void Remove(Item item) {
        _list.Remove(item);
        item.Container = null;
        CalculateWeight();
    }

    public bool TryAdd(Item item, int amount) {
        if (HasCapacityFor(item, amount) == false) {
            Log.Warning($"No capacity for {item} ({item.Weight} {Weight}/{MaxWeight})");
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Red, Duration = 2, Text = "Cannot transfer, exceeds container weight limit"
            });
            return false;
        }

        Item splitItem = item.SplitStack(amount);
        if (TryAdd(splitItem)) {
            return true;
        }

        TryAdd(splitItem);

        return false;
    }

    public bool TryAdd(Item? item) {
        if (item == null) {
            Log.Warning("Tried to add null item :(");
            return false;
        }

        if (HasCapacityFor(item) == false) {
            Log.Warning($"No capacity for {item} ({item.Weight} {Weight}/{MaxWeight})");
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Red, Duration = 2, Text = "Cannot transfer, exceeds container weight limit"
            });
            return false;
        }

        //todo there is a bug here where StackSize can/will exceed StackLimit, not doing enough
        if (item.IsStackable) {
            for (int i = 0; i < _list.Count; i++) {
                if (_list[i].Def != item.Def) continue;
                //todo there is a bug here where StackSize can/will exceed StackLimit, not doing enough
                _list[i].StackSize += item.StackSize;
                item.StackSize = 0;
                item.Container?.Remove(item);
                item.Destroy();
                CalculateWeight();
                return true;
            }
        }

        item.Container?.Remove(item);
        item.Container = this;
        _list.Add(item);
        CalculateWeight();
        return true;
    }

    public bool Contains(ItemDef def, int amountWanted) {
        int amount = 0;
        foreach (Item item in _list) {
            if (item.Def == def) {
                amount += item.StackSize;
            }
        }

        return amount >= amountWanted;
    }

    public Item? Take(EntityDef def, int amount) {
        foreach (Item item in _list) {
            if (item.Def == def) {
                return Take(item, amount);
            }
        }

        return null;
    }

    public Item? Take(Item item, int amount) {
        if (_list.Contains(item) == false) {
            Log.Error("ItemContainer doesn't contain " + item);
            return null;
        }

        if (amount > item.StackSize) {
            Log.Error("Tried to get " + amount + " of " + item + " while only having " + item.StackSize);
            amount = item.StackSize;
        }

        if (amount == item.StackSize) {
            Remove(item);
            return item;
        }

        Item splitItem = item.SplitStack(amount);
        CalculateWeight();

        return splitItem;
    }
}