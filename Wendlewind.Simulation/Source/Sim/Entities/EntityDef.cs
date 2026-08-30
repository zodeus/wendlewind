namespace Wendlewind.Sim.Entities;

public class EntityDef : Def {
    public virtual EntityType EntityType => throw new NotImplementedException($"EntityType not set for class {GetType().Name}");
    public Type EntityClass = null!;
    public Type? UiClass;
    public List<BaseStat> BaseStats = new();
    public string? TexturePath;
    public float IconFrameRate = 6;
    public bool IconPingPong = true;
}
