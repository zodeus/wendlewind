namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.DevConsole;

/// <summary>
/// In-game developer console for running commands.
/// Toggle with ~ key, type /commands to execute.
/// </summary>
public sealed class DevConsole : Panel
{
    private const int MaxLogLines = 100;
    private const int ConsoleHeight = 400;
    
    private readonly TextBox _inputField;
    private readonly VerticalStackPanel _logPanel;
    private readonly ScrollViewer _logScroll;
    private readonly List<string> _commandHistory = [];
    private readonly Desktop _desktop;
    private int _historyIndex = -1;
    private KeyboardState _previousKeyState;
    
    public bool IsOpen { get; private set; }
    
    public event Action? OnClose;

    public DevConsole(Desktop desktop)
    {
        _desktop = desktop;
        Background = new SolidBrush(new Color(15, 15, 20, 240));
        BorderThickness = new Thickness(0, 0, 0, 3);
        Border = new SolidBrush(new Color(80, 60, 40));
        Width = Core.ReferenceResolution.X;
        Height = ConsoleHeight;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Padding = new Thickness(10);

        var mainLayout = new VerticalStackPanel { Spacing = 8 };

        // Header
        var header = new HorizontalStackPanel { Spacing = 10 };
        header.Widgets.Add(new Label
        {
            Text = "DEVELOPER CONSOLE",
            TextColor = new Color(200, 160, 100),
            Font = BaseContent.Fonts.Default.Normal
        });
        header.Widgets.Add(new Label
        {
            Text = "| Press ~ or ESC to close",
            TextColor = new Color(120, 120, 120),
            Font = BaseContent.Fonts.Default.Small
        });
        mainLayout.Widgets.Add(header);

        // Separator
        mainLayout.Widgets.Add(new HorizontalSeparator { Color = new Color(60, 50, 40) });

        // Log area
        _logPanel = new VerticalStackPanel { Spacing = 2 };
        _logScroll = new ScrollViewer
        {
            Content = _logPanel,
            Height = ConsoleHeight - 130,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true
        };
        mainLayout.Widgets.Add(_logScroll);

        // Input area
        var inputContainer = new HorizontalStackPanel { Spacing = 8 };
        inputContainer.Widgets.Add(new Label
        {
            Text = ">",
            TextColor = new Color(100, 200, 100),
            VerticalAlignment = VerticalAlignment.Center
        });
        
        _inputField = new TextBox
        {
            Width = Core.ReferenceResolution.X - 60,
            TextColor = new Color(220, 220, 220),
            Background = new SolidBrush(new Color(25, 25, 30)),
            FocusedBackground = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(8, 4),
            HintText = "Type /help for commands...",
            AcceptsKeyboardFocus = true
        };
        inputContainer.Widgets.Add(_inputField);

        mainLayout.Widgets.Add(inputContainer);
        Widgets.Add(mainLayout);

        // Initial welcome message
        LogInfo("Developer Console initialized. Type /help for available commands.");
        
        Visible = false;
    }

    public void Open()
    {
        IsOpen = true;
        Visible = true;
        _inputField.Text = "";
        _previousKeyState = Keyboard.GetState();
        
        // Set focus using the Desktop
        _desktop.FocusedKeyboardWidget = _inputField;
    }

    public void Close()
    {
        IsOpen = false;
        Visible = false;
        OnClose?.Invoke();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    /// <summary>
    /// Call this every frame to handle keyboard input.
    /// </summary>
    public void UpdateInput()
    {
        if (!IsOpen) return;
        
        var currentKeyState = Keyboard.GetState();
        
        // Check for key presses (just pressed this frame)
        if (WasKeyJustPressed(Keys.Enter, currentKeyState))
        {
            ExecuteCommand();
        }
        else if (WasKeyJustPressed(Keys.Up, currentKeyState))
        {
            NavigateHistory(-1);
        }
        else if (WasKeyJustPressed(Keys.Down, currentKeyState))
        {
            NavigateHistory(1);
        }
        else if (WasKeyJustPressed(Keys.Escape, currentKeyState))
        {
            Close();
        }
        
        _previousKeyState = currentKeyState;
    }
    
    private bool WasKeyJustPressed(Keys key, KeyboardState current)
    {
        return current.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }

    private void NavigateHistory(int direction)
    {
        if (_commandHistory.Count == 0) return;

        _historyIndex += direction;
        _historyIndex = Math.Clamp(_historyIndex, 0, _commandHistory.Count - 1);
        _inputField.Text = _commandHistory[_historyIndex];
        _inputField.CursorPosition = _inputField.Text?.Length ?? 0;
    }

    private void ExecuteCommand()
    {
        var input = _inputField.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;

        // Add to history
        _commandHistory.Add(input);
        if (_commandHistory.Count > 50)
            _commandHistory.RemoveAt(0);
        _historyIndex = _commandHistory.Count;

        // Log the command
        LogCommand(input);

        // Parse and execute
        try
        {
            var result = DevConsoleCommands.Execute(input);
            if (result == "CLEAR")
            {
                Clear();
            }
            else if (!string.IsNullOrEmpty(result))
            {
                LogResult(result);
            }
        }
        catch (Exception ex)
        {
            LogError($"Error: {ex.Message}");
            LogError($"Error stack trace: {ex.StackTrace}");
        }

        _inputField.Text = "";
        
        // Re-focus the input field to keep typing
        _desktop.FocusedKeyboardWidget = _inputField;
        
        ScrollToBottom();
    }

    public void LogCommand(string text)
    {
        AddLogLine($"> {text}", new Color(100, 200, 100));
    }

    public void LogResult(string text)
    {
        // Split multi-line results
        foreach (var line in text.Split('\n'))
        {
            AddLogLine(line, new Color(200, 200, 200));
        }
    }

    public void LogInfo(string text)
    {
        AddLogLine(text, new Color(150, 180, 220));
    }

    public void LogError(string text)
    {
        AddLogLine(text, new Color(255, 100, 100));
    }

    public void LogSuccess(string text)
    {
        AddLogLine(text, new Color(100, 255, 150));
    }

    private void AddLogLine(string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            TextColor = color,
            Wrap = true,
            Width = Core.ReferenceResolution.X - 50
        };
        _logPanel.Widgets.Add(label);

        // Trim old lines
        while (_logPanel.Widgets.Count > MaxLogLines)
        {
            _logPanel.Widgets.RemoveAt(0);
        }

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        // Force layout update and scroll
        _logScroll.InvalidateMeasure();
        _logScroll.ScrollPosition = new Point(0, int.MaxValue);
    }

    public void Clear()
    {
        _logPanel.Widgets.Clear();
        LogInfo("Console cleared.");
    }
}
