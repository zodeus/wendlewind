namespace Wendlewind.Utils;

/// <summary>
/// Stable, process-independent mixing. Do not use <c>HashCode.Combine</c> or
/// <c>string.GetHashCode</c> — both are randomized per process in modern .NET.
/// </summary>
public static class SeedUtility
{
    public static int Mix(int a, int b)
    {
        unchecked
        {
            uint x = (uint)a;
            x ^= (uint)b + 0x9E3779B9u + (x << 6) + (x >> 2);
            return (int)x;
        }
    }

    public static int Mix(int a, int b, int c) => Mix(Mix(a, b), c);

    public static int StableHash(string text)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return (int)hash;
        }
    }

    public static int EncounterSeed(int runSeed, string zoneMoniker, int stage) =>
        Mix(runSeed, StableHash(zoneMoniker), stage);
}
