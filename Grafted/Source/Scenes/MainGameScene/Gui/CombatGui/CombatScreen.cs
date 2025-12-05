
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;


namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public class CombatScreen : VerticalStackPanel, IDisposable
{
    private readonly ZoneGui _gui;
    private readonly GameContext _context;
    private readonly ScrollViewer _combatLog;
    private readonly GameHud _gameHud;
    private readonly CombatPartyPanel _playerPartyPanel;
    private readonly CombatPartyPanel _opponentPartyPanel;
    private readonly CombatControlPanel _controlPanel;
    private readonly PawnBodyPanel _pawnBodyView;
    private readonly PawnBodyPanel _enemyPawnBodyView;
    private Button _playerQueuedPotionSlot;
    private Item? _playerQueuedPotion;
    private Button _enemyQueuedPotionSlot;
    private Item? _enemyQueuedPotion;
    private readonly HorizontalStackPanel _potionQueuePanel;
    private readonly Label _tickLabel;
    private Window? _combatLogWindow;

    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatScreen(ZoneGui gui, GameContext context)
    {
        _gui = gui;
        _context = context;
        Encounter.StateChangedAction += CombatStateChangedAction;
        Encounter.CombatHandler!.CombatLogMessageAdded += AddCombatLogEntry;
        Encounter.CombatHandler!.EventOccured += PrintDamage;
        _gameHud = new GameHud(gui, context)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 5, 0, 0)
        };

        _playerPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.PlayerPawns, HorizontalAlignment.Right);
        Grid.SetRow(_playerPartyPanel, 1);
        Grid.SetColumn(_playerPartyPanel, 0);
        
        _opponentPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.EnemyPawns, HorizontalAlignment.Left);
        Grid.SetRow(_opponentPartyPanel, 1);
        Grid.SetColumn(_opponentPartyPanel, 2);
        
        _controlPanel = new CombatControlPanel(Encounter)
        {
            MinWidth = 190,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetRow(_controlPanel, 2);
        Grid.SetColumn(_controlPanel, 0);

        _pawnBodyView = new PawnBodyPanel(gui, Encounter.PlayerPawns.First().Body)
        {
            Height = 1300, MinWidth = 600
        };

        _enemyPawnBodyView = new PawnBodyPanel(gui, Encounter.EnemyPawns.First().Body)
        {
            HorizontalAlignment = HorizontalAlignment.Left
            , MinWidth = 600
        };
        _combatLog = new ScrollViewer
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 1450,
            Height = 620,
            Content = new VerticalStackPanel { Padding = new Thickness(0), Spacing = 12 }
        };

        _playerQueuedPotionSlot = new Button(BaseContent.Styles.Button.Icon)
        {
            Width = 64, Height = 64
        };
        _playerQueuedPotionSlot.Click += (_, _) => Encounter.CombatHandler?.DeQueuedItemForPawn(Encounter.PlayerPawns[0]);
        _enemyQueuedPotionSlot = new Button(BaseContent.Styles.Button.Icon)
        {
            Width = 64, Height = 64
        };
        var separator = new VerticalSeparator { HorizontalAlignment = HorizontalAlignment.Center };
        _potionQueuePanel = new HorizontalStackPanel
        {
            Margin = new Thickness(5, 20, 5, 5),
            Widgets =
            {
                _playerQueuedPotionSlot,
                separator,
                _enemyQueuedPotionSlot,
            }
        };
        SetProportionType(separator, ProportionType.Fill);
        _tickLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Margin = new Thickness(0, 15, 0, 15),
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "0", TextColor = Color.DarkGoldenrod
        };

        var logsButton = new Button(BaseContent.Styles.Button.Normal)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Logs"
            }
        };
        logsButton.TouchDown += (_, _) =>
        {
            _combatLogWindow ??= new Window { Content = _combatLog };

            if (_combatLogWindow?.IsPlaced == true)
            {
                _combatLogWindow.Close();
                return;
            }

            _combatLogWindow!.Show(gui.Desktop);
            _combatLogWindow.Arrange(new Rectangle(0, 0, Core.ReferenceResolution.X, Core.ReferenceResolution.Y));
            var windowWidth = _combatLogWindow.ActualBounds.Width;
            var centerX = (Core.ReferenceResolution.X - windowWidth) / 2;
            _combatLogWindow.Left = centerX;
            _combatLogWindow.Top = 370;
        };
        VerticalStackPanel centerColumn = new()
        {
            Margin = new Thickness(10,0,10,0),
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                _controlPanel,
                _potionQueuePanel,
                _tickLabel,
                logsButton
            }
        };
        Grid.SetRow(centerColumn, 1);
        Grid.SetColumn(centerColumn, 1);

        var panel = new Panel()
        {
            Height = 1300,
            Widgets = { _enemyPawnBodyView }
        };
        Grid.SetRow(panel, 2);
        Grid.SetColumn(panel, 2);
        Grid grid = new()
        {
            Margin = new Thickness(30, 0, 30, 30),
            HorizontalAlignment = HorizontalAlignment.Center,
            RowSpacing = 0,
            DefaultRowProportion = Proportion.Auto,
            DefaultColumnProportion = Proportion.Auto,
        };
        var playerBodyPanel = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Widgets = { _pawnBodyView }
        };
        Grid.SetRow(playerBodyPanel, 2);
        Grid.SetColumn(playerBodyPanel, 0);
        
        var centerBodyPanel = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(centerBodyPanel, 2);
        Grid.SetColumn(centerBodyPanel, 1);
        
        grid.Widgets.Add(_playerPartyPanel);
        grid.Widgets.Add(centerColumn);
        grid.Widgets.Add(_opponentPartyPanel);
        grid.Widgets.Add(playerBodyPanel);
        grid.Widgets.Add(centerBodyPanel);
        grid.Widgets.Add(panel);

        Widgets.Add(_gameHud);
        Widgets.Add(grid);
    }

    private void PrintDamage(CombatEvent combatEvent)
    {
        var color = combatEvent.Type switch
        {
            CombatEventType.Damage => new Color(186, 22, 0),
            CombatEventType.Block => new Color(0, 150, 237),
            CombatEventType.Dodge => new Color(0, 150, 237),
            CombatEventType.Miss => Color.Orange,
            CombatEventType.Heal => Color.GreenYellow,
            CombatEventType.Buff => Color.GreenYellow,
            CombatEventType.Debuff => new Color(237, 51, 0),
            CombatEventType.Death => Color.AntiqueWhite,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Route damage text to the appropriate body widget
        var partyPanel = combatEvent.Target.PawnType == PawnType.Player 
            ? _playerPartyPanel 
            : _opponentPartyPanel;
        
        var combatPanel = partyPanel.GetPanelForPawn(combatEvent.Target);
        if (combatPanel?.BodyWidget != null)
        {
            combatPanel.BodyWidget.AddDamageText(combatEvent.BodyPart, combatEvent.Text, color, 3f);
        }
    }

    private void CombatStateChangedAction(EncounterState state)
    {
        switch (state)
        {
            case EncounterState.InProgress:
                break;
            case EncounterState.Finished:
                ShowCombatSummary();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void ShowCombatSummary()
    {
        var summaryWindow = new CombatSummaryWindow(Encounter, () => Encounter.Zone.CombatResults());
        summaryWindow.ShowModal(_gui.Desktop);
    }

    public void Update(float deltaTime)
    {
        if (Encounter.CombatHandler?.ItemQueuedFor(Encounter.PlayerPawns[0]) is { } potion)
        {
            if (!Equals(_playerQueuedPotion, potion))
            {
                _playerQueuedPotion = potion;
                _playerQueuedPotionSlot.Content = new Image { Background = new TextureRegion(potion.Icon), Width = 64, Height = 64 };
            }
        }
        else if (_playerQueuedPotionSlot.Content != null)
        {
            _playerQueuedPotion = null;
            _playerQueuedPotionSlot.Content = null;
        }

        if (Encounter.CombatHandler?.ItemQueuedFor(Encounter.EnemyPawns[0]) is { } enemyPotion)
        {
            if (!Equals(_enemyQueuedPotion, enemyPotion))
            {
                _enemyQueuedPotion = enemyPotion;
                _enemyQueuedPotionSlot.Content = new Image { Background = new TextureRegion(enemyPotion.Icon), Width = 64, Height = 64 };
            }
        }
        else if (_enemyQueuedPotionSlot.Content != null)
        {
            _enemyQueuedPotion = null;
            _enemyQueuedPotionSlot.Content = null;
        }

        _tickLabel.Text = $"{_context.CurrentZone?.ActiveEncounter?.Ticks}";
        _gameHud.Update();
        _playerPartyPanel.Update(deltaTime);
        _opponentPartyPanel.Update(deltaTime);
        _pawnBodyView.Update();
        _enemyPawnBodyView.Update();
    }

    private void AddCombatLogEntry(string? detailedMessage)
    {
        var panel = (VerticalStackPanel)_combatLog.Content;
        if (panel.Widgets.Count > 300)
        {
            panel.Widgets.RemoveAt(panel.Widgets.Count - 1);
        }

        Label label = new(BaseContent.Styles.Label.Small)
        {
            Width = 1600,
            Text = detailedMessage, Wrap = true, Margin = new Thickness(0, 10, 0, 0)
        };

        panel.Widgets.Insert(0, label);
    }

    public void Dispose()
    {
        Encounter.StateChangedAction -= CombatStateChangedAction;
        Encounter.CombatHandler!.CombatLogMessageAdded -= AddCombatLogEntry;
        Encounter.CombatHandler!.EventOccured -= PrintDamage;
    }
}