using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

    public Item? TryAdd(Item item, int amount) {
        if (HasCapacityFor(item, amount) == false) {
            Log.Warning($"No capacity for {item} ({item.Weight} {Weight}/{MaxWeight})");
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Red, Duration = 2, Text = "Cannot transfer, exceeds container weight limit"
            });
            return item;
        }

        Item itemToAdd = item;
        Item? itemToReturn = null;
        if (item.StackSize > amount) {
            item.StackSize -= amount;
            item.Container?.CalculateWeight();
            itemToAdd = EntityGenerator.CreateEntity<Item>(item.ItemDef, amount);
            itemToReturn = item;
        }

        if (TryAdd(itemToAdd) is { } returnedItem) {
            if (itemToReturn == null) {
                itemToReturn = returnedItem;
            }
            else {
                itemToReturn.StackSize += returnedItem.StackSize;
                returnedItem.Destroy();
            }
        }

        return itemToReturn;
    }

    public Item? TryAdd(Item? item) {
        if (item == null) {
            Log.Warning("Tried to add null item :(");
            return item;
        }

        if (HasCapacityFor(item) == false) {
            Log.Warning($"No capacity for {item} ({item.Weight} {Weight}/{MaxWeight})");
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Red, Duration = 2, Text = "Cannot transfer, exceeds container weight limit"
            });
            return item;
        }

        if (item.IsStackable) {
            for (int i = 0; i < _list.Count; i++) {
                if (_list[i].Def != item.Def) continue;
                _list[i].StackSize += item.StackSize;
                item.StackSize = 0;
                item.Container?.Remove(item);
                item.Destroy();
                CalculateWeight();
                return null;
            }
        }

        item.Container?.Remove(item);
        item.Container = this;
        _list.Add(item);
        CalculateWeight();
        return null;
    }
}