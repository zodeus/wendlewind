using Grafted.Sim.Persistence;
using Grafted.Sim.Zones.Handlers;

namespace Grafted.Sim;

public abstract class TownStructure :IExposable{
    public TownStructureDef Def = null!;
    public int Id = -1;
    public Town Town = null!;

    public virtual void Tick() { }

    public virtual void Initialize() { }
    public virtual void ExposeData() {
        Scribe_Values.Look(ref Id, "Id");
        Scribe_Defs.Look(ref Def!, "Def");
        Scribe_References.Look(ref Town!, "Town");
    }
}