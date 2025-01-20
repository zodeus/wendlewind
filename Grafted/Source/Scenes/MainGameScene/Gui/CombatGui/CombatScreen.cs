using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

public class CombatScreen : VerticalStackPanel
{
    private readonly GameContext _context;
    private readonly ScrollViewer _combatLog;
    private readonly GameHud _gameHud;
    private readonly CombatPartyPanel _playerPartyPanel;
    private readonly CombatPartyPanel _opponentPartyPanel;
    private readonly CombatControlPanel _controlPanel;
    private readonly PawnBodyPanel _pawnBodyView;
    private ImageButton _playerQueuedPotionSlot;
    private Item? _playerQueuedPotion = null;
    private ImageButton _enemyQueuedPotionSlot;
    private Item? _enemyQueuedPotion = null;
    private readonly HorizontalStackPanel _potionQueuePanel;
    private readonly Label _tickLabel;

    private Encounter Encounter => _context.CurrentZone!.ActiveEncounter!;

    public CombatScreen(ZoneGui gui, GameContext context)
    {
        _context = context;
        Encounter.StateChangedAction += CombatStateChangedAction();
        Encounter.CombatRecord.LogMessageAddedAction += message => { AddCombatLogEntry(message.Text); };
        _gameHud = new GameHud(gui, context)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 5, 0, 0)
        };

        _playerPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.PlayerPawns, HorizontalAlignment.Right)
        {
            GridRow = 1, GridColumn = 0
        };
        _opponentPartyPanel = new CombatPartyPanel(gui, Encounter, Encounter.EnemyPawns, HorizontalAlignment.Left)
        {
            GridRow = 1, GridColumn = 2
        };
        _controlPanel = new CombatControlPanel(Encounter)
        {
            GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _pawnBodyView = new PawnBodyPanel(gui, Encounter.PlayerPawns.First().Body)
        {
            GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Right,
            Width = 1200, Height = 1300,
            Margin = new Thickness(0, 0, 30, 0),
        };
        _combatLog = new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Width = 1600,
            VerticalAlignment = VerticalAlignment.Top,
            Content = new VerticalStackPanel { Padding = new Thickness(0), Spacing = 12 }
        };

        _playerQueuedPotionSlot = new ImageButton(BaseContent.Styles.Button.Icon)
        {
            Width = 64, Height = 64
        };
        _playerQueuedPotionSlot.Click += (_, _) => Encounter.CombatHandler?.DeQueuedItemForPawn(Encounter.PlayerPawns[0]);
        _enemyQueuedPotionSlot = new ImageButton(BaseContent.Styles.Button.Icon)
        {
            Width = 64, Height = 64
        };
        _potionQueuePanel = new HorizontalStackPanel
        {
            Margin = new Thickness(5, 20, 5, 5),
            Proportions =
            {
                Proportion.Auto, Proportion.Fill, Proportion.Auto
            },
            Widgets =
            {
                _playerQueuedPotionSlot,
                new VerticalSeparator { HorizontalAlignment = HorizontalAlignment.Center },
                _enemyQueuedPotionSlot,
            }
        };
        _tickLabel = new Label(BaseContent.Styles.Label.Normal)
        {
            Margin = new Thickness(0, 15, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "0", TextColor = Color.DarkGoldenrod
        };

        VerticalStackPanel centerColumn = new()
        {
            Spacing = 0, GridRow = 1, GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets =
            {
                _controlPanel,
                _potionQueuePanel,
                _tickLabel
            }
        };

        HorizontalStackPanel logPanel = new()
        {
            //BorderThickness = new Thickness(1),Border = new SolidBrush(Color.Orange),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            GridRow = 2, GridColumn = 2,
            Width = 1600, Height = 1300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(15, 15, 8, 15),
            Margin = new Thickness(30, 0, 0, 0),
            Widgets =
            {
                _combatLog
            }
        };

        Grid grid = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            RowSpacing = 10,
            DefaultRowProportion = Proportion.Auto,
            DefaultColumnProportion = Proportion.Auto,
            Widgets =
            {
                _playerPartyPanel, centerColumn, _opponentPartyPanel,
                _pawnBodyView, logPanel
            }
        };
        Widgets.Add(_gameHud);
        Widgets.Add(grid);
    }

    private Action<EncounterState> CombatStateChangedAction()
    {
        return state =>
        {
            switch (state)
            {
                case EncounterState.InProgress:
                    break;
                case EncounterState.Finished:
                    _controlPanel.ShowContinueButton();
                    _potionQueuePanel.Visible = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        };
    }

    public void Update()
    {
        if (Encounter.CombatHandler?.ItemQueuedFor(Encounter.PlayerPawns[0]) is { } potion)
        {
            if (!Equals(_playerQueuedPotion, potion))
            {
                _playerQueuedPotion = potion;
                _playerQueuedPotionSlot.Image = new TextureRegion(potion.Icon);
            }
        }
        else if (_playerQueuedPotionSlot.Image != null)
        {
            _playerQueuedPotion = null;
            _playerQueuedPotionSlot.Image = null;
        }

        if (Encounter.CombatHandler?.ItemQueuedFor(Encounter.EnemyPawns[0]) is { } enemyPotion)
        {
            if (!Equals(_enemyQueuedPotion, enemyPotion))
            {
                _enemyQueuedPotion = enemyPotion;
                _enemyQueuedPotionSlot.Image = new TextureRegion(enemyPotion.Icon);
            }
        }
        else if (_enemyQueuedPotionSlot.Image != null)
        {
            _enemyQueuedPotion = null;
            _enemyQueuedPotionSlot.Image = null;
        }

        _tickLabel.Text = $"{_context.CurrentZone?.ActiveEncounter?.Ticks}";
        _gameHud.Update();
        _playerPartyPanel.Update();
        _opponentPartyPanel.Update();
        _pawnBodyView.Update();
    }

    private void AddCombatLogEntry(string text, Color? color = null)
    {
        if (((VerticalStackPanel)_combatLog.Content).Widgets.Count > 300)
        {
            ((VerticalStackPanel)_combatLog.Content).Widgets.RemoveAt(0);
        }

        Label label = new(BaseContent.Styles.Label.Small) { Text = text, Wrap = true, Margin = new Thickness(0, 10, 0, 0) };
        if (color != null)
        {
            label.TextColor = color.Value;
        }

        ((VerticalStackPanel)_combatLog.Content).Widgets.Add(label);
        _combatLog.ScrollPosition = _combatLog.ScrollMaximum + new Point(0, 500);
    }
}