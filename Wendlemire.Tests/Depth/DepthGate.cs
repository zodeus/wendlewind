namespace Wendlemire.Tests.Depth;

internal static class DepthGate
{
    public static bool FullSuite =>
        string.Equals(Environment.GetEnvironmentVariable("WENDLEMIRE_DEPTH"), "1", StringComparison.Ordinal);
}
