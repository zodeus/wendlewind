using Wendlemire.NetCode;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaHud : HorizontalStackPanel
{
    private readonly Label _gold;
    private readonly Label _wins;
    private readonly Label _lives;
    private readonly GameContext _context;

    public ArenaHud(GameContext context, ArenaRankDisplay? rank = null)
    {
        _context = context;
        Spacing = 24;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Padding = new Thickness(12, 6);

        if (rank is { } current)
        {
            Widgets.Add(new RankBadge(current, badgeSize: 64));
        }

        _gold = StatLabel();
        _wins = StatLabel();
        _lives = StatLabel();
        Widgets.Add(_gold);
        Widgets.Add(_wins);
        Widgets.Add(_lives);
        Refresh();
    }

    public void Refresh()
    {
        var run = _context.ArenaRun;
        if (run == null)
        {
            return;
        }

        _gold.Text = $"Gold {run.Gold}";
        _wins.Text = $"Wins {run.Wins}/{ArenaRun.WinsToFinish}";
        _lives.Text = $"Lives {run.LivesRemaining}";
    }

    private static Label StatLabel()
    {
        return new Label(BaseContent.Styles.Label.Medium)
        {
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
