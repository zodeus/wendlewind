using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaRunEndScreen : VerticalStackPanel
{
    public ArenaRunEndScreen(
        GameContext context,
        Action onMenu,
        ArenaRunRecord? finished = null,
        ArenaRankDisplay? rank = null)
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

        if (rank is { } current)
        {
            Widgets.Add(new RankBadge(current, badgeSize: 128)
            {
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        if (finished?.RatingBefore is int before && finished.RatingAfter is int after)
        {
            var beforeRank = ArenaRank.FromRating(before, Math.Max(0, (rank?.RatedRuns ?? 1) - (finished.RankApplied ? 1 : 0)));
            var delta = finished.RatingDelta ?? (after - before);
            var sign = delta >= 0 ? "+" : "";
            Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
            {
                Text = finished.RankApplied
                    ? $"{beforeRank.Label}  →  {rank?.Label ?? after.ToString()}  ({sign}{delta})"
                    : "Rank unchanged (no player opponents)",
                TextColor = !finished.RankApplied ? Color.Gray : delta >= 0 ? Color.LightGreen : Color.IndianRed,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        var menu = new CursorButton(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label { Text = "Main Menu", HorizontalAlignment = HorizontalAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Center
        };
        menu.Click += (_, _) => onMenu();
        Widgets.Add(menu);
    }
}
