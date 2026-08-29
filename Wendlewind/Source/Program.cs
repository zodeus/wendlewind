using System.IO;
using System.Runtime.InteropServices;

namespace Wendlewind;

public static class Program {
    [STAThread]
    private static void Main() {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            WriteCrash(args.ExceptionObject as Exception ?? new Exception(args.ExceptionObject?.ToString()));
        };

        try
        {
            using Core game = new();
            game.Run();
        }
        catch (Exception ex)
        {
            WriteCrash(ex);
            throw;
        }
    }

    private static void WriteCrash(Exception ex)
    {
        try
        {
            AttachConsole();
            Console.Error.WriteLine();
            Console.Error.WriteLine(ex);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), ex.ToString());
        }
        catch
        {
            // last-resort: don't hide the original crash
        }
    }

    private static void AttachConsole()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        if (GetConsoleWindow() == IntPtr.Zero)
        {
            AllocConsole();
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
