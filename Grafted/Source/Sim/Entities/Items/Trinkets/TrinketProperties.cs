namespace Grafted.Sim.Entities.Items.Trinkets;

public class TrinketProperties
{
    public TrinketType Type = TrinketType.Invalid;

    [UsedImplicitly] public Type? HandlerClass;
}