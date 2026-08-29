namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets;

/// <summary>
/// A reusable popup component that displays a list of selectable items with icons.
/// Automatically closes when the mouse moves too far away.
/// </summary>
public sealed class SelectionPopup<T>
{
    private readonly Desktop _desktop;
    private Window? _popup;
    private const int PopupCloseDistance = 10;
    private const int MaxItemsPerColumn = 4;

    public bool IsOpen => _popup?.IsPlaced == true;

    public SelectionPopup(Desktop desktop)
    {
        _desktop = desktop;
    }

    /// <summary>
    /// Shows the popup with the given items.
    /// </summary>
    /// <param name="items">Items to display in the popup</param>
    /// <param name="iconSelector">Function to get the icon for each item</param>
    /// <param name="onSelect">Callback when an item is selected</param>
    public void Show(IEnumerable<T> items, Func<T, Texture2D> iconSelector, Action<T> onSelect)
    {
        if (_popup?.IsPlaced == true)
        {
            return;
        }

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return;
        }

        _popup = new Window
        {
            Title = null,
            Background = null,
            Padding = new Thickness(0)
        };
        _popup.TitlePanel.Visible = false;

        var columnsPanel = new HorizontalStackPanel { Spacing = 0 };
        VerticalStackPanel? currentColumn = null;

        for (int i = 0; i < itemList.Count; i++)
        {
            // Start a new column every MaxItemsPerColumn items
            if (i % MaxItemsPerColumn == 0)
            {
                currentColumn = new VerticalStackPanel { Spacing = 0 };
                columnsPanel.Widgets.Add(currentColumn);
            }

            var item = itemList[i];
            var itemButton = new CursorButton(BaseContent.Styles.Button.Dark)
            {
                Content = new Image
                {
                    Background = new TextureRegion(iconSelector(item)),
                    Width = BaseContent.IconSizes.Default,
                    Height = BaseContent.IconSizes.Default
                }
            };

            var capturedItem = item;
            itemButton.Click += (_, _) =>
            {
                onSelect(capturedItem);
                Close();
            };

            currentColumn!.Widgets.Add(itemButton);
        }

        _popup.Content = columnsPanel;
        var uiPos = Core.ScreenToUi(Mouse.GetState().Position);
        _popup.Show(_desktop, uiPos);
    }

    /// <summary>
    /// Closes the popup if it's open.
    /// </summary>
    public void Close()
    {
        _popup?.Close();
        _popup = null;
    }

    /// <summary>
    /// Should be called each frame to check if the mouse has moved too far from the popup
    /// and close it if necessary.
    /// </summary>
    public void Update()
    {
        if (_popup?.IsPlaced != true)
        {
            return;
        }

        var contentPos = Core.ScreenToUi(Mouse.GetState().Position);
        var boundsOffset = new Point(
            (int)(Core.UiOffset.X / Core.UiScale),
            (int)(Core.UiOffset.Y / Core.UiScale)
        );
        var uiMousePos = new Point(contentPos.X + boundsOffset.X, contentPos.Y + boundsOffset.Y);

        var contentBounds = _popup.Content?.Bounds ?? _popup.Bounds;
        const int styleBuffer = 20;
        var popupBounds = new Rectangle(
            _popup.Left,
            _popup.Top,
            contentBounds.Width + styleBuffer,
            contentBounds.Height + styleBuffer
        );

        var expandedBounds = new Rectangle(
            popupBounds.X - PopupCloseDistance,
            popupBounds.Y - PopupCloseDistance,
            popupBounds.Width + PopupCloseDistance * 2,
            popupBounds.Height + PopupCloseDistance * 2
        );

        if (!expandedBounds.Contains(uiMousePos.X, uiMousePos.Y))
        {
            Close();
        }
    }
}
