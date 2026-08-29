using Wendlewind.NetCode;

namespace Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

public sealed class TestSimSelectorScreen : VerticalStackPanel
{
    private readonly GameContext _context;
    private readonly Label _attackerLabel;
    private readonly Label _defenderLabel;
    private readonly TextBox _seedField;

    public TestSimSelectorScreen(GameContext context)
    {
        _context = context;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Spacing = 16;
        Padding = new Thickness(24);
        MinWidth = 520;

        Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = "Test Simulation",
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Pick two build templates and a seed, then start a fully autonomous fight.",
            TextColor = new Color(180, 180, 180),
            Wrap = true,
            MaxWidth = 480,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _attackerLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = TestSimSettings.AttackerBuildId,
            TextColor = Color.White
        };
        _defenderLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = TestSimSettings.DefenderBuildId,
            TextColor = Color.White
        };

        Widgets.Add(CreateCycleRow("Attacker", _attackerLabel, id => TestSimSettings.AttackerBuildId = id));
        Widgets.Add(CreateCycleRow("Defender", _defenderLabel, id => TestSimSettings.DefenderBuildId = id));

        _seedField = new TextBox
        {
            Width = 160,
            Text = TestSimSettings.Seed.ToString(),
            TextColor = Color.White,
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(8, 4)
        };
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Seed",
                    VerticalAlignment = VerticalAlignment.Center
                },
                _seedField
            }
        });

        var start = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Start" }
        };
        start.Click += (_, _) => Start();
        Widgets.Add(start);
    }

    public void Update()
    {
    }

    private Widget CreateCycleRow(string title, Label valueLabel, Action<string> assign)
    {
        var cycle = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Next" }
        };
        cycle.Click += (_, _) =>
        {
            var ids = BuildTemplates.All.Select(t => t.BuildId).ToList();
            var current = ids.IndexOf(valueLabel.Text);
            var next = ids[(current + 1 + ids.Count) % ids.Count];
            valueLabel.Text = next;
            assign(next);
        };

        return new HorizontalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = title,
                    MinWidth = 90,
                    VerticalAlignment = VerticalAlignment.Center
                },
                valueLabel,
                cycle
            }
        };
    }

    private void Start()
    {
        if (int.TryParse(_seedField.Text, out var seed))
        {
            TestSimSettings.Seed = seed;
        }

        TestSimLauncher.StartEncounter(_context);
    }
}
