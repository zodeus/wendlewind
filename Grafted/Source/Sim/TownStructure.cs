namespace Grafted.Sim;

public abstract class TownStructure {
    public TownStructureDef Def = null!;
    public int Id = -1;
    public Town Town = null!;

    public virtual void Tick() { }

    public virtual void Initialize() { }
}