using System.Runtime.InteropServices;

namespace Grafted;

public static class Program {
    [STAThread]
    private static void Main() {
        using Core game = new();
        game.Run();
    }
}