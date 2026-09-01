using Wendlewind.NetCode;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaResultsScreen : VerticalStackPanel
{
    public ArenaResultsScreen(GameContext context, Action onContinue, ArenaRankDisplay? rank = null)
    {
        Spacing = 16;
        Padding = new Thickness(24);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        var run = context.ArenaRun ?? throw new InvalidOperationException("Results requires an ArenaRun.");
        var title = run.LastFightWon ? "Victory" : "Defeat";
        var color = run.LastFightWon ? Color.Goldenrod : Color.IndianRed;

        Widgets.Add(new ArenaHud(context, rank));
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = title,
            TextColor = color,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"Opponent: {run.LastOpponentPlayerId ?? "unknown"}",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = $"+{run.LastGoldDelta} gold",
            TextColor = Color.LightGreen,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var continueButton = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label
            {
                Text = "Choose a merchant",
                HorizontalAlignment = HorizontalAlignment.Center
            },
            HorizontalAlignment = HorizontalAlignment.Center
        };
        continueButton.Click += (_, _) => onContinue();
        Widgets.Add(continueButton);
    }
}
