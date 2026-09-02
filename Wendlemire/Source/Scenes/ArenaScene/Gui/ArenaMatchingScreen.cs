namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaMatchingScreen : VerticalStackPanel
{
    private readonly EllipsisLabel? _status;

    public ArenaMatchingScreen(string? error, Action? onRetry)
    {
        Spacing = 16;
        Padding = new Thickness(24);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        if (error == null)
        {
            _status = new EllipsisLabel(BaseContent.Styles.Label.Huge)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                TextColor = Color.Goldenrod,
                BaseText = "Finding opponent"
            };
            Widgets.Add(_status);
            return;
        }

        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "Match failed",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.IndianRed
        });

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = error,
            Wrap = true,
            Width = 700,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        if (onRetry != null)
        {
            var retry = new CursorButton(BaseContent.Styles.Button.LargeGold)
            {
                Content = new Label { Text = "Back to prep", HorizontalAlignment = HorizontalAlignment.Center },
                HorizontalAlignment = HorizontalAlignment.Center
            };
            retry.Click += (_, _) => onRetry();
            Widgets.Add(retry);
        }
    }

    public void Update(float deltaTime)
    {
        _status?.Update(deltaTime);
    }
}
