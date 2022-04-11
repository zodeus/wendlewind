using System;
using System.Runtime.InteropServices;
using Grafted.Debug;
using Grafted.Utils;

namespace Grafted;

public static class Program {
    [STAThread]
    private static void Main(string[] args) {
        AttachConsole(-1);
        if (args.Contains("quick-play")) {
            DebugSettings.QuickPlay = true;
        }
        using Core game = new();
        game.Run();
    }

    [DllImport("kernel32.dll")]
    static extern bool AttachConsole(int dwProcessId);
}