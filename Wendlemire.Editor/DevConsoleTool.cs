using Wendlemire.Sim.Debug;
using Num = System.Numerics;

namespace Wendlemire.Editor;

public sealed class DevConsoleTool
{
    private readonly GameContext _context;
    private readonly List<(string Text, Num.Vector4 Color)> _log = [];
    private readonly List<string> _history = [];
    private string _input = "";
    private int _historyIndex;

    public DevConsoleTool(GameContext context)
    {
        _context = context;
        _log.Add(("Type /help for available commands.", new Num.Vector4(0.7f, 0.7f, 0.7f, 1f)));
    }

    public void Draw()
    {
        ImGui.Text("Dev Console");
        ImGui.SameLine();
        ImGui.TextDisabled("Shared with Test Sim. Commands mutate the live GameContext.");
        ImGui.BeginChild("console-log", new Num.Vector2(0, -ImGui.GetFrameHeightWithSpacing() - 8), ImGuiChildFlags.Borders);
        foreach (var (text, color) in _log)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
        }

        if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 8)
        {
            ImGui.SetScrollHereY(1f);
        }

        ImGui.EndChild();

        ImGui.SetNextItemWidth(-1);
        var flags = ImGuiInputTextFlags.EnterReturnsTrue;
        if (ImGui.InputText("##cmd", ref _input, 256, flags))
        {
            Submit();
        }

        if (ImGui.IsItemFocused())
        {
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
            {
                Recall(-1);
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            {
                Recall(1);
            }
        }
    }

    private void Submit()
    {
        var input = _input.Trim();
        if (input.Length == 0)
        {
            return;
        }

        _history.Add(input);
        if (_history.Count > 50)
        {
            _history.RemoveAt(0);
        }

        _historyIndex = _history.Count;
        _log.Add(($"> {input}", new Num.Vector4(0.95f, 0.78f, 0.35f, 1f)));

        try
        {
            var result = DevConsoleCommands.Execute(_context, input);
            if (result == "CLEAR")
            {
                _log.Clear();
            }
            else if (!string.IsNullOrEmpty(result))
            {
                var color = result.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                            result.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase)
                    ? new Num.Vector4(0.95f, 0.35f, 0.35f, 1f)
                    : new Num.Vector4(0.85f, 0.85f, 0.85f, 1f);
                foreach (var line in result.Replace("\r\n", "\n").Split('\n'))
                {
                    _log.Add((line, color));
                }
            }
        }
        catch (Exception ex)
        {
            _log.Add(($"Error: {ex.Message}", new Num.Vector4(0.95f, 0.35f, 0.35f, 1f)));
        }

        _input = "";
        ImGui.SetKeyboardFocusHere(-1);
    }

    private void Recall(int delta)
    {
        if (_history.Count == 0)
        {
            return;
        }

        _historyIndex = Math.Clamp(_historyIndex + delta, 0, _history.Count);
        _input = _historyIndex < _history.Count ? _history[_historyIndex] : "";
    }
}
