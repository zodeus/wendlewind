namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaMatchingScreen : VerticalStackPanel
{
    public ArenaMatchingScreen(string? error, Action? onRetry)
    {
        Spacing = 16;
        Padding = new Thickness(24);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = error == null ? "Finding opponent..." : "Match failed",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = error == null ? Color.Goldenrod : Color.IndianRed
        });

        if (error != null)
        {
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
    }
}
