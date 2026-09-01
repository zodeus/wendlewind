namespace Wendlemire.Editor;

public static class Program
{
    [STAThread]
    private static void Main()
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        using var app = new EditorApp();
        app.Run();
    }
}
