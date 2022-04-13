using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities;

public interface IEntityContainer : IEnumerable<Entity> { }

public class ItemContainer : IEntityContainer, IEnumerable<Item> {
    private readonly List<Item> _list;

    public ItemContainer() {
        _list = new List<Item>();
    }

    public void Tick() {
        for (int index = _list.Count - 1; index >= 0; index--) {
            Entity entity = _list[index];
            entity.Tick();
            if (entity.IsDestroyed) {
                _list.RemoveAt(index);
            }
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
    }

    public void TryAdd(Item item) {
        if (item.IsStackable) {
            for (int i = 0; i < _list.Count; i++) {
                if (_list[i].Def != item.Def) continue;
                _list[i].StackSize += item.StackSize;
                item.StackSize = 0;
                item.Destroy();
                return;
            }
        }

        //todo this is a hack
        if (item.Def.Moniker == "WoodenStick" && _list.Any(i => i.Def.Moniker == "WoodenStick")) {
            return;
        }

        _list.Add(item);
    }
}