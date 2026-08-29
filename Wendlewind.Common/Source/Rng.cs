namespace Wendlewind;

/// <summary>
/// Process-wide RNG slots. Simulation uses <see cref="IRng"/> / <c>GameContext.Rng</c>.
/// Presentation uses <see cref="Visual"/>.
/// </summary>
public static class Rng
{
    public static Random Current { get; set; } = new();
    public static Random Visual { get; set; } = new();
}
