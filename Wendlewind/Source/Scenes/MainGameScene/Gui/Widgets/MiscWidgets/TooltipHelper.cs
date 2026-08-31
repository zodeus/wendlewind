namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public enum TooltipPlacement
{
    FollowMouse,
    BottomCorner
}

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
    private static TooltipPlacement _placement = TooltipPlacement.FollowMouse;

    private const int OffsetX = 16;
    private const int OffsetY = 16;
    private const int CornerMargin = 24;
    private const int CustomMaxHeight = 380;

    /// <summary>
    /// Shows a simple tooltip with a title and optional description.
    /// </summary>
    public static void Show(Desktop desktop, string title, string? description = null, Widget? owner = null)
    {
        _placement = TooltipPlacement.FollowMouse;
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
        ApplyWindowChrome();
        
        _shouldBeVisible = true;
        _currentDesktop = desktop;
        ShowWindow(desktop);
    }

    /// <summary>
    /// Shows a tooltip with custom widget content.
    /// </summary>
    public static void ShowCustom(
        Desktop desktop,
        Widget content,
        Widget? owner = null,
        TooltipPlacement placement = TooltipPlacement.FollowMouse)
    {
        _placement = placement;
        _currentOwner = owner;
        
        EnsureWindowCreated();
        
        _customContent = WrapCustomContent(content);
        _window!.Content = _customContent;
        StackPanel.SetProportionType(_customContent, ProportionType.Auto);
        _isCustomContent = true;
        ApplyWindowChrome();
        
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
            _placement = TooltipPlacement.FollowMouse;
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

        if (!_window.IsPlaced && _currentDesktop != null)
        {
            ShowWindow(_currentDesktop);
            return;
        }

        if (_window.IsPlaced)
        {
            ApplyPlacement();
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

        _window = new TooltipWindow
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Margin = new Thickness(0),
            Padding = new Thickness(10, 3, 10, 10),
            Content = _defaultContent,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        _window.TitlePanel.Visible = false;
        ApplyWindowChrome();
    }

    private static Widget WrapCustomContent(Widget content)
    {
        if (content is ScrollViewer viewer)
        {
            var current = viewer.MaxHeight ?? CustomMaxHeight;
            viewer.MaxHeight = Math.Min(current, CustomMaxHeight);
            return viewer;
        }

        if (_customContent is ScrollViewer existing && existing.Content == content)
        {
            return existing;
        }

        return new ScrollViewer
        {
            Content = content,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            MaxHeight = CustomMaxHeight
        };
    }

    private static void ApplyWindowChrome()
    {
        if (_window == null)
        {
            return;
        }

        _window.Background = Stylesheet.Current.Atlas[
            _placement == TooltipPlacement.BottomCorner
                ? BaseContent.Styles.Atlas.Panel.MediumFrame
                : BaseContent.Styles.Atlas.Panel.IconFrame];
        _window.MaxHeight = _isCustomContent ? CustomMaxHeight : null;
        _window.HorizontalAlignment = HorizontalAlignment.Left;
        _window.VerticalAlignment = VerticalAlignment.Top;
    }

    private static void ShowWindow(Desktop desktop)
    {
        if (_window!.IsPlaced)
        {
            _window.Close();
        }

        var origin = GetPlacementOrigin();
        _window.Show(desktop, origin);
        ApplyPlacement();
    }

    private static void ApplyPlacement()
    {
        if (_window == null)
        {
            return;
        }

        _window.Arrange(new Rectangle(0, 0, Core.ReferenceResolution.X, Core.ReferenceResolution.Y));
        var width = _window.ActualBounds.Width;
        var height = _window.ActualBounds.Height;
        var screenW = Core.ReferenceResolution.X;
        var screenH = Core.ReferenceResolution.Y;

        if (_placement == TooltipPlacement.FollowMouse)
        {
            var (uiX, uiY) = GetMouseUiPosition();
            var left = uiX + OffsetX;
            var top = uiY + OffsetY;
            if (left + width > screenW)
            {
                left = uiX - width - OffsetX;
            }

            if (top + height > screenH)
            {
                top = uiY - height - OffsetY;
            }

            _window.Left = Math.Clamp(left, 0, Math.Max(0, screenW - width));
            _window.Top = Math.Clamp(top, 0, Math.Max(0, screenH - height));
            return;
        }

        var pinRight = IsNearRightEdge();

        _window.Left = pinRight
            ? screenW - width - CornerMargin
            : CornerMargin;
        _window.Top = screenH - height - CornerMargin;
    }

    private static Point GetPlacementOrigin()
    {
        if (_placement == TooltipPlacement.BottomCorner)
        {
            return IsNearRightEdge()
                ? new Point(Core.ReferenceResolution.X - CornerMargin, Core.ReferenceResolution.Y - CornerMargin)
                : new Point(CornerMargin, Core.ReferenceResolution.Y - CornerMargin);
        }

        var (uiX, uiY) = GetMouseUiPosition();
        return new Point(uiX + OffsetX, uiY + OffsetY);
    }

    private static bool IsNearRightEdge()
    {
        if (_currentOwner is { ActualBounds.Width: > 0 })
        {
            return _currentOwner.ActualBounds.Center.X >= Core.ReferenceResolution.X / 2;
        }

        var (uiX, _) = GetMouseUiPosition();
        return uiX >= Core.ReferenceResolution.X / 2;
    }

    private static (int x, int y) GetMouseUiPosition()
    {
        var screenPos = Mouse.GetState().Position;
        var uiX = (int)((screenPos.X - Core.UiOffset.X) / Core.UiScale);
        var uiY = (int)((screenPos.Y - Core.UiOffset.Y) / Core.UiScale);
        return (uiX, uiY);
    }

    private sealed class TooltipWindow : Window
    {
        public override Widget? HitTest(Point p) => null;
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
    public static T WithTooltip<T>(
        this T widget,
        Func<Widget> contentFactory,
        TooltipPlacement placement = TooltipPlacement.FollowMouse) where T : Widget
    {
        Widget? content = null;
        widget.MouseEntered += (_, _) =>
        {
            if (widget.Desktop != null)
            {
                content ??= contentFactory();
                TooltipHelper.ShowCustom(widget.Desktop, content, widget, placement);
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
