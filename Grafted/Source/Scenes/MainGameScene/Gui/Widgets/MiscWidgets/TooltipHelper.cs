namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

/// <summary>
/// A reusable tooltip system that displays information when hovering over UI elements.
/// Use the static methods to show/hide tooltips, or attach to widgets using the extension methods.
/// </summary>
public static class TooltipHelper
{
    private static Window? _window;
    private static VerticalStackPanel? _defaultContent;
    private static Label? _titleLabel;
    private static Label? _descriptionLabel;
    private static Widget? _customContent;
    private static bool _isCustomContent;
    private static bool _shouldBeVisible;
    private static Desktop? _currentDesktop;
    private static Widget? _currentOwner; // Track which widget owns the current tooltip

    private const int OffsetX = 15;
    private const int OffsetY = 15;

    /// <summary>
    /// Shows a simple tooltip with a title and optional description.
    /// </summary>
    public static void Show(Desktop desktop, string title, string? description = null, Widget? owner = null)
    {
        _currentOwner = owner;
        
        EnsureWindowCreated();
        
        // Switch to default content if we were showing custom
        if (_isCustomContent && _customContent != null)
        {
            _window!.Content = _defaultContent;
            _isCustomContent = false;
        }
        
        _titleLabel!.Text = title;
        _descriptionLabel!.Text = description ?? "";
        _descriptionLabel.Visible = !string.IsNullOrEmpty(description);
        
        _shouldBeVisible = true;
        _currentDesktop = desktop;
        ShowWindow(desktop);
    }

    /// <summary>
    /// Shows a tooltip with custom widget content.
    /// </summary>
    public static void ShowCustom(Desktop desktop, Widget content, Widget? owner = null)
    {
        _currentOwner = owner;
        
        EnsureWindowCreated();
        
        _customContent = content;
        _window!.Content = content;
        _isCustomContent = true;
        
        _shouldBeVisible = true;
        _currentDesktop = desktop;
        ShowWindow(desktop);
    }

    /// <summary>
    /// Hides the tooltip, but only if the requesting widget is the current owner.
    /// This handles event ordering when moving between tooltip-enabled items.
    /// </summary>
    public static void Hide(Widget? owner = null)
    {
        // Only hide if the requesting widget is the current owner,
        // or if no owner tracking is being used (owner is null and _currentOwner is null)
        if (owner == null || _currentOwner == null || owner == _currentOwner)
        {
            _shouldBeVisible = false;
            _currentOwner = null;
            _window?.Close();
        }
        // If a different widget is requesting hide, ignore it - 
        // a new tooltip was already shown for a different widget
    }

    /// <summary>
    /// Updates the tooltip position to follow the mouse. Call this in Update() while hovering.
    /// </summary>
    public static void UpdatePosition()
    {
        if (!_shouldBeVisible || _window == null) return;
        
        // Re-show if needed (handles rapid mouse movement between items)
        if (!_window.IsPlaced && _currentDesktop != null)
        {
            var (uiX, uiY) = GetMouseUiPosition();
            _window.Show(_currentDesktop, new Point(uiX + OffsetX, uiY + OffsetY));
            return;
        }
        
        if (_window.IsPlaced)
        {
            var (uiX, uiY) = GetMouseUiPosition();
            _window.Left = uiX + OffsetX;
            _window.Top = uiY + OffsetY;
        }
    }

    /// <summary>
    /// Returns true if the tooltip is currently visible.
    /// </summary>
    public static bool IsVisible => _shouldBeVisible && _window?.IsPlaced == true;

    private static void EnsureWindowCreated()
    {
        if (_window != null) return;

        _titleLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            TextColor = Color.White
        };
        
        _descriptionLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(180, 180, 180),
            Wrap = true,
            MaxWidth = 250
        };

        _defaultContent = new VerticalStackPanel { Spacing = 4 };
        _defaultContent.Widgets.Add(_titleLabel);
        _defaultContent.Widgets.Add(_descriptionLabel);

        _window = new Window
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Margin = new Thickness(0),
            Padding = new Thickness(10, 3, 10, 10),
            Content = _defaultContent
        };
        _window.TitlePanel.Visible = false;
    }

    private static void ShowWindow(Desktop desktop)
    {
        var (uiX, uiY) = GetMouseUiPosition();
        
        // Always close and re-show to avoid race conditions with IsPlaced state
        if (_window!.IsPlaced)
        {
            _window.Close();
        }
        
        _window.Show(desktop, new Point(uiX + OffsetX, uiY + OffsetY));
    }

    private static (int x, int y) GetMouseUiPosition()
    {
        var screenPos = Mouse.GetState().Position;
        var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
        var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);
        return (uiX, uiY);
    }
}

/// <summary>
/// Extension methods for easily attaching tooltips to widgets.
/// </summary>
public static class TooltipExtensions
{
    /// <summary>
    /// Attaches a simple tooltip to a widget that shows on hover.
    /// </summary>
    public static T WithTooltip<T>(this T widget, string title, string? description = null) where T : Widget
    {
        widget.MouseEntered += (_, _) =>
        {
            if (widget.Desktop != null)
                TooltipHelper.Show(widget.Desktop, title, description, widget);
        };
        widget.MouseLeft += (_, _) => TooltipHelper.Hide(widget);
        return widget;
    }

    /// <summary>
    /// Attaches a tooltip with custom content to a widget that shows on hover.
    /// </summary>
    public static T WithTooltip<T>(this T widget, Func<Widget> contentFactory) where T : Widget
    {
        Widget? content = null;
        widget.MouseEntered += (_, _) =>
        {
            if (widget.Desktop != null)
            {
                content ??= contentFactory();
                TooltipHelper.ShowCustom(widget.Desktop, content, widget);
            }
        };
        widget.MouseLeft += (_, _) => TooltipHelper.Hide(widget);
        return widget;
    }

    /// <summary>
    /// Attaches a dynamic tooltip to a widget. The title/description are evaluated each time the tooltip is shown.
    /// </summary>
    public static T WithDynamicTooltip<T>(this T widget, Func<string> titleGetter, Func<string?>? descriptionGetter = null) where T : Widget
    {
        widget.MouseEntered += (_, _) =>
        {
            if (widget.Desktop != null)
                TooltipHelper.Show(widget.Desktop, titleGetter(), descriptionGetter?.Invoke(), widget);
        };
        widget.MouseLeft += (_, _) => TooltipHelper.Hide(widget);
        return widget;
    }
}
