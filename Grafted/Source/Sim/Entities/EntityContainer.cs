using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities;

public interface IEntityContainer : IEnumerable<Entity> { }

public class ItemContainer : IEntityContainer, IEnumerable<Item> {
    private readonly List<Item> _list;
    private int _weight = 0;

    public ItemContainer() {
        _list = new List<Item>();
    }

    public int Weight => _weight;

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
        CalculateWeight();
    }

    public void TryAdd(Item? item) {
        if (item == null) {
            Log.Warning("Tried to add null item :(");
            return;
        }

        if (item.IsStackable) {
            for (int i = 0; i < _list.Count; i++) {
                if (_list[i].Def != item.Def) continue;
                _list[i].StackSize += item.StackSize;
                item.StackSize = 0;
                item.Destroy();
                CalculateWeight();
                return;
            }
        }

        item.Container = this;
        _list.Add(item);
        CalculateWeight();
    }

    public void TryTransfer(Item item) {
        item.Container?.Remove(item);
        item.Container = null;

        TryAdd(item);
    }
}