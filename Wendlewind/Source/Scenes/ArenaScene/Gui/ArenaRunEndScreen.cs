namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaRunEndScreen : VerticalStackPanel
{
    public ArenaRunEndScreen(GameContext context, Action onMenu)
    {
        Spacing = 16;
        Padding = new Thickness(24);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        var run = context.ArenaRun ?? throw new InvalidOperationException("Run end requires an ArenaRun.");
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = run.IsVictory ? "Arena Champion" : "Run Over",
            TextColor = run.IsVictory ? Color.Goldenrod : Color.IndianRed,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"{run.Wins} wins  /  {run.Losses} losses",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"{run.Gold} gold remaining",
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var menu = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Main Menu", HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Center
        };
        menu.Click += (_, _) => onMenu();
        Widgets.Add(menu);
    }
}
