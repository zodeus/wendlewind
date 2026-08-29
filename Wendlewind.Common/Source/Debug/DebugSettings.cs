namespace Wendlewind.Debug;

public static class DebugSettings {
    public static int CombatSpeed { get; set; } = 1;
    public static bool TestSimMode { get; set; }

    public static void DetectLaunchFlags()
    {
        if (Environment.GetEnvironmentVariable("WENDLEWIND_TEST_SIM") == "1")
        {
            TestSimMode = true;
            return;
        }

        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, "--test-sim", StringComparison.OrdinalIgnoreCase))
            {
                TestSimMode = true;
                return;
            }
        }
    }
}