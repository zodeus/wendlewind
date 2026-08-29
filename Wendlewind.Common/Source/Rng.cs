namespace Wendlewind;

/// <summary>
/// Process-wide RNG slots. Simulation code should go through
/// <c>GameContext.Random</c>, which owns the instance for the active context
/// and keeps <see cref="Current"/> in sync for Common helpers.
/// </summary>
public static class Rng
{
    public static Random Current { get; set; } = new();
    public static Random Visual { get; set; } = new();
}
