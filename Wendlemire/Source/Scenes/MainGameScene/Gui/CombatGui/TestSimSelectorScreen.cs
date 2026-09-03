using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

public sealed class TestSimSelectorScreen : Panel
{
    private const int CardSpacing = 10;

    private readonly BaseGui _gui;
    private readonly GameContext _context;
    private readonly VerticalStackPanel _selectorColumn;
    private readonly TextBox _seedField;
    private readonly Label _pickingHint;
    private readonly HorizontalStackPanel _filterRow;
    private readonly ScrollViewer _catalogScroll;
    private readonly VerticalStackPanel _catalogBody;
    private readonly List<TestSimBuildCard> _cards = [];
    private readonly List<(CursorButton Button, BuildStage? Stage)> _filters = [];
    private TestSimBuildCard _attackerSlot;
    private TestSimBuildCard _defenderSlot;
    private readonly Panel _attackerHost;
    private readonly Panel _defenderHost;

    private PawnPreparationPanel? _prepPanel;
    private BuildStage? _filter;
    private bool _pickingAttacker = true;
    private int _cardsPerRow = -1;

    public TestSimSelectorScreen(BaseGui gui, GameContext context)
    {
        _gui = gui;
        _context = context;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        BuildCatalog.EnsureGenerated(TestSimSettings.CatalogSeed);

        _selectorColumn = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Spacing = 12,
            Padding = new Thickness(24, 16)
        };

        _selectorColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Large)
        {
            Text = "Test Simulation",
            TextColor = Color.Goldenrod,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        _selectorColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Click a card to set the highlighted fighter. Right-click to set the other.",
            TextColor = new Color(180, 180, 180),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _attackerSlot = CreateSlotCard(Resolve(TestSimSettings.AttackerBuildId), pickingAttacker: true);
        _defenderSlot = CreateSlotCard(Resolve(TestSimSettings.DefenderBuildId), pickingAttacker: false);
        _attackerHost = WrapSlot("Attacker", _attackerSlot);
        _defenderHost = WrapSlot("Defender", _defenderSlot);

        _seedField = new TextBox
        {
            Width = 140,
            Text = TestSimSettings.Seed.ToString(),
            TextColor = Color.White,
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(8, 4)
        };

        var configure = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Configure" }
        };
        configure.Click += (_, _) => OpenPreparation();

        var start = new CursorButton(BaseContent.Styles.Button.Gold)
        {
            Content = new Label(BaseContent.Styles.Label.Normal) { Text = "Start" }
        };
        start.Click += (_, _) => Start();

        _pickingHint = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Picking attacker",
            TextColor = new Color(180, 180, 180),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var vsColumn = new VerticalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 180,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Large)
                {
                    Text = "VS",
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                _pickingHint,
                new HorizontalStackPanel
                {
                    Spacing = 8,
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
                },
                configure,
                start
            }
        };

        _selectorColumn.Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = { _attackerHost, vsColumn, _defenderHost }
        });

        _filterRow = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        AddFilterButton("All", null);
        AddFilterButton("Signature", BuildStage.Signature);
        foreach (var stage in BuildStages.Generated)
        {
            AddFilterButton(stage.Label(), stage);
        }

        var reroll = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Reroll Generated" }
        };
        reroll.Click += (_, _) => RerollGenerated();
        _filterRow.Widgets.Add(reroll);
        _selectorColumn.Widgets.Add(_filterRow);

        _catalogBody = new VerticalStackPanel
        {
            Spacing = CardSpacing,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _catalogScroll = new ScrollViewer
        {
            Content = _catalogBody,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true
        };
        _selectorColumn.Widgets.Add(_catalogScroll);
        VerticalStackPanel.SetProportionType(_catalogScroll, ProportionType.Fill);

        RebuildCatalog();
        RefreshSelection();
        Widgets.Add(_selectorColumn);
    }

    public void Update()
    {
        _prepPanel?.Update();
        if (_prepPanel != null)
        {
            return;
        }

        var perRow = CardsPerRow();
        if (perRow != _cardsPerRow)
        {
            RebuildCatalog();
        }
    }

    private void AddFilterButton(string label, BuildStage? stage)
    {
        var button = new CursorButton(BaseContent.Styles.Button.Dark)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = label,
                TextColor = stage is { } s ? TestSimBuildCard.StageColor(s) : Color.White
            }
        };
        button.Click += (_, _) =>
        {
            _filter = stage;
            RefreshFilters();
            RebuildCatalog();
        };
        _filters.Add((button, stage));
        _filterRow.Widgets.Add(button);
        RefreshFilters();
    }

    private void RefreshFilters()
    {
        foreach (var (button, stage) in _filters)
        {
            button.Enabled = true;
            button.Background = Stylesheet.Current.Atlas[_filter == stage
                ? BaseContent.Styles.Atlas.Panel.MediumFrameBright
                : BaseContent.Styles.Atlas.Panel.MediumFrame];
        }
    }

    private TestSimBuildCard CreateSlotCard(BuildSnapshot snapshot, bool pickingAttacker)
    {
        var card = new TestSimBuildCard(snapshot, slot: true, (_, _) =>
        {
            _pickingAttacker = pickingAttacker;
            RefreshSelection();
        });
        return card;
    }

    private static Panel WrapSlot(string title, Widget card)
    {
        var column = new VerticalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = title,
                    TextColor = Color.Goldenrod,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                card
            }
        };

        return new Panel
        {
            Widgets = { column },
            Width = TestSimBuildCard.SlotWidth
        };
    }

    private void Assign(BuildSnapshot snapshot, bool rightClick)
    {
        var toAttacker = rightClick ? !_pickingAttacker : _pickingAttacker;
        BuildCatalog.Pin(snapshot);
        if (toAttacker)
        {
            if (TestSimSettings.AttackerBuildId != snapshot.BuildId)
            {
                TestSimSettings.AttackerOverride = null;
            }

            TestSimSettings.AttackerBuildId = snapshot.BuildId;
            ReplaceSlot(_attackerHost, ref _attackerSlot, snapshot, pickingAttacker: true);
        }
        else
        {
            TestSimSettings.DefenderBuildId = snapshot.BuildId;
            ReplaceSlot(_defenderHost, ref _defenderSlot, snapshot, pickingAttacker: false);
        }

        RefreshSelection();
    }

    private void ReplaceSlot(Panel host, ref TestSimBuildCard slot, BuildSnapshot snapshot, bool pickingAttacker)
    {
        var column = (VerticalStackPanel)host.Widgets[0];
        slot.RemoveFromParent();
        slot = CreateSlotCard(snapshot, pickingAttacker);
        column.Widgets.Add(slot);
    }

    private void RebuildCatalog()
    {
        _cardsPerRow = CardsPerRow();
        _cards.Clear();
        _catalogBody.Widgets.Clear();

        var groups = BuildCatalog.All
            .Where(b => _filter == null || BuildCatalog.StageOf(b) == _filter)
            .GroupBy(BuildCatalog.StageOf)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            _catalogBody.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = group.Key.Label(),
                TextColor = TestSimBuildCard.StageColor(group.Key),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 2)
            });

            HorizontalStackPanel? row = null;
            var index = 0;
            foreach (var build in group)
            {
                if (index % _cardsPerRow == 0)
                {
                    row = new HorizontalStackPanel
                    {
                        Spacing = CardSpacing,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    _catalogBody.Widgets.Add(row);
                }

                var card = new TestSimBuildCard(build, slot: false, Assign);
                _cards.Add(card);
                row!.Widgets.Add(card);
                index++;
            }
        }

        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var card in _cards)
        {
            card.Refresh(TestSimSettings.AttackerBuildId, TestSimSettings.DefenderBuildId, _pickingAttacker);
        }

        _attackerSlot.Refresh(TestSimSettings.AttackerBuildId, TestSimSettings.DefenderBuildId, _pickingAttacker);
        _defenderSlot.Refresh(TestSimSettings.AttackerBuildId, TestSimSettings.DefenderBuildId, _pickingAttacker);
        _pickingHint.Text = _pickingAttacker ? "Picking attacker" : "Picking defender";
    }

    private int CardsPerRow()
    {
        var width = Math.Max(ActualBounds.Width, Bounds.Width) - 64;
        if (width <= 0)
        {
            return 5;
        }

        return Math.Max(1, width / (TestSimBuildCard.CatalogWidth + CardSpacing));
    }

    private void RerollGenerated()
    {
        TestSimSettings.CatalogSeed++;
        BuildCatalog.Regenerate(TestSimSettings.CatalogSeed);
        RebuildCatalog();
    }

    private static BuildSnapshot Resolve(string buildId)
    {
        if (BuildCatalog.TryGet(buildId, out var snapshot))
        {
            return snapshot;
        }

        var fallback = BuildTemplates.All[0];
        return fallback;
    }

    private void OpenPreparation()
    {
        if (_prepPanel != null)
        {
            return;
        }

        var build = TestSimSettings.AttackerOverride ?? BuildCatalog.Get(TestSimSettings.AttackerBuildId);
        BuildSnapshotFactory.Apply(_context.PlayerPawn, BuildTemplates.WithAllWeapons(build));

        _prepPanel = new PawnPreparationPanel(_gui, _context.PlayerPawn)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var backButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
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
