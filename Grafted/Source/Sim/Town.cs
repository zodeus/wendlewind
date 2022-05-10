using System.Collections.Generic;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class Town : IExposable, IIdentityProvider {
    private Dictionary<TownStructureDef, TownStructure> _structures = new();

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

    public void ExposeData() {
        Scribe_Defs.Look(ref ZoneDef!, "ZoneDef");
        Scribe_Collections.Look(ref _structures!, "Structures", LookMode.Def, LookMode.Deep);

    }

    public string GetUniqueId() {
        return ZoneDef.Moniker + "-Town";
    }
}