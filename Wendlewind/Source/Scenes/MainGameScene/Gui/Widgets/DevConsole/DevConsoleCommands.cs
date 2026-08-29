namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.DevConsole;

/// <summary>
/// Client wrapper around the portable command executor.
/// </summary>
public static class DevConsoleCommands
{
    public static string Execute(string input)
    {
        if (Core.Context == null)
        {
            return "Error: No game context available.";
        }

        return Wendlewind.Sim.Debug.DevConsoleCommands.Execute(Core.Context, input);
    }
}
