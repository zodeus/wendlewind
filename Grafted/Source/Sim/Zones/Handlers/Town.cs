using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Zones.Handlers;

public class Town : ZoneHandler, IIdentityProvider {
    private Dictionary<TownStructureDef, TownStructure> _structures = new();
    private string _id = "invalid";

    public override void Tick() {
        foreach (TownStructure structure in _structures.Values) {
            structure.Tick();
        }
    }

    public override void OnEnter() { }
    public override void OnExit() { }

    public override void Initialize(World world, Zone zone) {
        foreach (TownStructureDef structureDef in DefRepository<TownStructureDef>.Defs) {
            AddStructure(TownGenerator.GenerateStructure(structureDef, this));
        }

        _id = zone.Def.Moniker + "-Town";

        base.Initialize(world, zone);
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

    public override void ExposeData() {
        Scribe_Collections.Look(ref _structures!, "Structures", LookMode.Def, LookMode.Deep);
        base.ExposeData();
    }

    public string GetUniqueId() {
        return _id;
    }
}