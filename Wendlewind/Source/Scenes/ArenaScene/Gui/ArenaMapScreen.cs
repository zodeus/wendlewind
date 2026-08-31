using Wendlewind.Scenes.MainGameScene.Gui.Widgets.MapWidgets;

namespace Wendlewind.Scenes.ArenaScene.Gui;

public sealed class ArenaMapScreen : VerticalStackPanel
{
    private readonly ArenaHud _hud;

    public ArenaMapScreen(GameContext context, Action<MerchantDef> onMerchantPicked)
    {
        Spacing = 16;
        Padding = new Thickness(16);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        var run = context.ArenaRun ?? throw new InvalidOperationException("Map requires an ArenaRun.");
        _hud = new ArenaHud(context);

        Widgets.Add(_hud);
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Text = "Choose a merchant",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Goldenrod
        });
        Widgets.Add(BuildFightSpine(run));
        Widgets.Add(BuildMerchantRow(onMerchantPicked));
    }

    private static Widget BuildFightSpine(ArenaRun run)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        for (var i = 1; i <= ArenaRun.WinsToFinish; i++)
        {
            var state = i <= run.Wins
                ? MapNodeState.Completed
                : i == run.Wins + 1
                    ? MapNodeState.Current
                    : MapNodeState.Locked;
            var color = state switch
            {
                MapNodeState.Completed => new Color(80, 140, 80),
                MapNodeState.Current => new Color(232, 170, 0),
                _ => new Color(50, 50, 55)
            };

            row.Widgets.Add(new Panel
            {
                Width = 36,
                Height = 36,
                Background = new ColoredRegion(
                    Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.RoundWhite64],
                    color),
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = i.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            });
        }

        return row;
    }

    private static Widget BuildMerchantRow(Action<MerchantDef> onMerchantPicked)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        foreach (var merchant in DefRepository<MerchantDef>.Defs.Where(m => !m.IsGeneralStore))
        {
            var captured = merchant;
            var button = new CursorButton(BaseContent.Styles.Button.Normal)
            {
                Content = new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = captured.Label,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Width = 160,
                Height = 80
            };
            button.Click += (_, _) => onMerchantPicked(captured);
            row.Widgets.Add(button);
        }

        return row;
    }

    public void Update()
    {
        _hud.Refresh();
    }
}
