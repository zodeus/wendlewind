using Wendlewind.NetCode;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

public sealed class TestSimSelectorScreen : Panel
{
    private readonly BaseGui _gui;
    private readonly GameContext _context;
    private readonly VerticalStackPanel _selectorColumn;
    private readonly TextBox _seedField;

    private PawnPreparationPanel? _prepPanel;

    public TestSimSelectorScreen(BaseGui gui, GameContext context)
    {
        _gui = gui;
        _context = context;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _selectorColumn = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
            Padding = new Thickness(24),
            MinWidth = 560
        };

        _selectorColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = "Test Simulation",
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _selectorColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Pick two build templates and a seed, then start a fully autonomous fight.",
            TextColor = new Color(180, 180, 180),
            Wrap = true,
            MaxWidth = 480,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _selectorColumn.Widgets.Add(CreateBuildPicker("Attacker", TestSimSettings.AttackerBuildId, id =>
        {
            if (TestSimSettings.AttackerBuildId == id)
            {
                return;
            }

            TestSimSettings.AttackerBuildId = id;
            // The hand-tuned player loadout no longer matches the chosen template, so discard it.
            TestSimSettings.AttackerOverride = null;
        }));
        _selectorColumn.Widgets.Add(CreateBuildPicker("Defender", TestSimSettings.DefenderBuildId, id =>
        {
            TestSimSettings.DefenderBuildId = id;
        }));

        _seedField = new TextBox
        {
            Width = 160,
            Text = TestSimSettings.Seed.ToString(),
            TextColor = Color.White,
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(8, 4)
        };
        _selectorColumn.Widgets.Add(new HorizontalStackPanel
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

        var configurePlayer = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Configure Player" }
        };
        configurePlayer.Click += (_, _) => OpenPreparation();
        _selectorColumn.Widgets.Add(configurePlayer);

        var start = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Start" }
        };
        start.Click += (_, _) => Start();
        _selectorColumn.Widgets.Add(start);

        Widgets.Add(_selectorColumn);
    }

    public void Update()
    {
        _prepPanel?.Update();
    }

    private Widget CreateBuildPicker(string title, string selectedId, Action<string> assign)
    {
        var ids = BuildTemplates.All.Select(t => t.BuildId).ToList();
        if (!ids.Contains(selectedId) && ids.Count > 0)
        {
            selectedId = ids[0];
            assign(selectedId);
        }

        var combo = new ComboView
        {
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var id in ids)
        {
            combo.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = id,
                TextColor = Color.White
            });
        }

        var selectedIndex = ids.IndexOf(selectedId);
        if (selectedIndex >= 0)
        {
            combo.SelectedIndex = selectedIndex;
        }

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex is int index && index >= 0 && index < ids.Count)
            {
                assign(ids[index]);
            }
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
                combo
            }
        };
    }

    private void OpenPreparation()
    {
        if (_prepPanel != null)
        {
            return;
        }

        // Populate the player pawn with the selected attacker build (or a previously saved hand-tuned override)
        // so the preparation screen shows the right potions/weapons/stance to tweak.
        var build = TestSimSettings.AttackerOverride ?? BuildTemplates.Get(TestSimSettings.AttackerBuildId);
        BuildSnapshotFactory.Apply(_context.PlayerPawn, build);

        _prepPanel = new PawnPreparationPanel(_gui, _context.PlayerPawn)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var backButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Back to Test Sim" }
        };
        backButton.Click += (_, _) => ClosePreparation();
        _prepPanel.SetControls(backButton);

        _selectorColumn.Visible = false;
        Widgets.Add(_prepPanel);
    }

    private void ClosePreparation()
    {
        if (_prepPanel == null)
        {
            return;
        }

        // Capture the hand-tuned loadout so it survives the world re-init that happens on Start.
        TestSimSettings.AttackerOverride =
            BuildSnapshotFactory.ToSnapshot(_context.PlayerPawn, "player", TestSimSettings.AttackerBuildId);

        _prepPanel.RemoveFromParent();
        _prepPanel = null;
        _selectorColumn.Visible = true;
    }

    private void Start()
    {
        if (_prepPanel != null)
        {
            ClosePreparation();
        }

        if (int.TryParse(_seedField.Text, out var seed))
        {
            TestSimSettings.Seed = seed;
        }

        TestSimLauncher.StartEncounter(_context);
    }
}
