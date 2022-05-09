using System.Collections.Generic;
using Grafted.Maths;

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