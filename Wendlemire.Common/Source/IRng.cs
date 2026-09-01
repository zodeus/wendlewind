namespace Wendlemire;

/// <summary>
/// Run-scoped random source. Simulation helpers take <see cref="AsRandom"/>.
/// Presentation stays on <see cref="Rng.Visual"/>.
/// </summary>
public interface IRng
{
    Random AsRandom { get; }
    int Next();
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
    float NextFloat();
    float NextFloat(float minValue, float maxValue);
    bool Chance(float chance);
}

public sealed class SimRng : IRng
{
    public Random AsRandom { get; }

    public SimRng(int seed) => AsRandom = new Random(seed);
    public SimRng(Random random) => AsRandom = random ?? throw new ArgumentNullException(nameof(random));

    public int Next() => AsRandom.Next();
    public int Next(int maxValue) => AsRandom.Next(maxValue);
    public int Next(int minValue, int maxValue) => AsRandom.Next(minValue, maxValue);
    public double NextDouble() => AsRandom.NextDouble();
    public float NextFloat() => AsRandom.NextFloat();
    public float NextFloat(float minValue, float maxValue) => AsRandom.NextFloat(minValue, maxValue);
    public bool Chance(float chance) => AsRandom.Chance(chance);
}
