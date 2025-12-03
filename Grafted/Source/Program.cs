using System.Runtime.InteropServices;

namespace Grafted;

public static class Program {
    [STAThread]
    private static void Main(string[] args) {
        if (args.Contains("quick-play")) {
            DebugSettings.QuickPlay = true;
        }
        using Core game = new();
        game.Run();
    }
}