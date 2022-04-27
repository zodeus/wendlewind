using System;
using System.Linq;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Gui.CombatWidgets;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;
using Label = Myra.Graphics2D.UI.Label;

namespace Grafted.Sim.Gui;

public class CombatGui : BaseGui {
    private readonly CombatEvent _combatEvent;
    private readonly ScrollViewer _combatLog;
    private readonly GameStatsPanel _gameStatsPanel;
    private readonly Label _turnLabel;
    private readonly CombatPartyPanel _playerPartyPanel;
    private readonly CombatPartyPanel _opponentPartyPanel;
    private readonly CombatControlPanel _controlPanel;
    private readonly PawnBodyPanel _pawnBodyView;
    private ImageButton _playerQueuedPotionSlot;
    private Item? _playerQueuedPotion = null;
    public CombatEvent CombatEvent => _combatEvent;

    public CombatGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        _combatEvent.StartAsCoroutine();
        _combatEvent.StateChangedAction += CombatStateChangedAction();
        _combatEvent.CombatRecord.LogMessageAddedAction += message => {
            AddCombatLogEntry(message.Text);
        };
        _gameStatsPanel = new GameStatsPanel {
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0, GridColumn = 0, GridColumnSpan = 3, Margin = new Thickness(0, 5, 0, 0)
        };

        _playerPartyPanel = new CombatPartyPanel(_combatEvent, combatEvent.PlayerPawns, HorizontalAlignment.Right) {
            GridRow = 1, GridColumn = 0
        };
        _opponentPartyPanel = new CombatPartyPanel(_combatEvent, combatEvent.EnemyPawns, HorizontalAlignment.Left) {
            GridRow = 1, GridColumn = 2
        };
        _controlPanel = new CombatControlPanel(_combatEvent) {
            GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _pawnBodyView = new PawnBodyPanel(combatEvent.PlayerPawns.First().Body, socket => {
            if (socket.AttachedPart != null) {
                ViewEntity(socket.AttachedPart);
            }
        }) {
            GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Right,
            Width = 810,
            Margin = new Thickness(0, 0, 30, 0),
        };
        _combatLog = new ScrollViewer {
            Content = new VerticalStackPanel() { Padding = new Thickness(0) }
        };

        _turnLabel = new Label {
            Font = BaseContent.Fonts.Fancy.Large, HorizontalAlignment = HorizontalAlignment.Center
        };
        VerticalStackPanel turnPane = new() {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(15),
            Widgets = {
                _turnLabel
            }
        };

        _playerQueuedPotionSlot = new ImageButton(BaseContent.Styles.Button.Icon) {
            Width = 24, Height = 24
        };
        _playerQueuedPotionSlot.Click += (_, _) => _combatEvent.DeQueuedPotionFor(_combatEvent.PlayerPawns[0]);

        VerticalStackPanel centerColumn = new() {
            Spacing = 0, GridRow = 1, GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = {
                turnPane, _controlPanel,
                new HorizontalSeparator(),
                new HorizontalStackPanel {
                    Margin = new Thickness(5, 20, 5, 5),
                    Proportions = {
                        Proportion.Auto, Proportion.Fill, Proportion.Auto
                    },
                    Widgets = {
                        _playerQueuedPotionSlot,
                        new VerticalSeparator() { HorizontalAlignment = HorizontalAlignment.Center },
                        new ImageButton(BaseContent.Styles.Button.Icon) {
                            Enabled = false,
                            Width = 24, Height = 24
                        },
                    }
                }
            }
        };

        HorizontalStackPanel logPanel = new() {
            //BorderThickness = new Thickness(1),Border = new SolidBrush(Color.Orange),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            GridRow = 2, GridColumn = 2,
            Height = 800,
            Width = 810,
            HorizontalAlignment = HorizontalAlignment.Left,
            DefaultProportion = Proportion.Fill,
            Padding = new Thickness(15, 15, 8, 15),
            Margin = new Thickness(30, 0, 0, 0),
            Widgets = {
                _combatLog
            }
        };

        Grid grid = new() {
            HorizontalAlignment = HorizontalAlignment.Center,
            RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, 
            DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                _gameStatsPanel,
                _playerPartyPanel, centerColumn, _opponentPartyPanel,
                _pawnBodyView, logPanel
            }
        };

        Desktop = new Desktop { Root = grid, HasExternalTextInput = true };
    }



    private Action<CombatState> CombatStateChangedAction() {
        return state => {
            switch (state) {
                case CombatState.TurnInteractive:
                    //(Pawn pawn, PawnTurnData turnData) = _combatEvent.CurrentTurn.PawnTurnData.Last();
                    //_controlPanel.EnableFor(pawn, turnData);
                    break;
                case CombatState.Turn:
                    //_controlPanel.ClearControls();
                    break;
                case CombatState.CombatFinished:
                    _controlPanel.ShowContinueButton();
                    break;
                case CombatState.TurnStart:
                    //ClearCombatLog();
                    break;
                case CombatState.Preparation:
                    break;
                case CombatState.TurnEnd:
                    break;
                case CombatState.CombatEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        };
    }

    public override void Update(float deltaTime) {
        if (_combatEvent.State == CombatState.CombatFinished && Input.IsKeyPressed(Keys.Enter)) {
            Core.Sim.Gui = new CombatResultsGui(_combatEvent);
        }

        if (_combatEvent.PotionQueuedFor(_combatEvent.PlayerPawns[0]) is { } potion) {
            if (_playerQueuedPotion != potion) {
                _playerQueuedPotionSlot.Image = new TextureRegion(potion.Icon);
            }
        }
        else if (_playerQueuedPotionSlot.Image != null) {
            _playerQueuedPotionSlot.Image = null;
        }

        _gameStatsPanel.Update();
        _playerPartyPanel.Update();
        _opponentPartyPanel.Update();
        _pawnBodyView.Update();
        _turnLabel.Text = $"Turn {_combatEvent.CurrentTurnNum.ToString().PadLeft(2, '0')}";
        base.Update(deltaTime);
    }

    private void AddCombatLogEntry(string text, Color? color = null) {
        if (((VerticalStackPanel) _combatLog.Content).ChildrenCount > 200) {
            ClearCombatLog();
        }

        Label label = new() { Text = text, Font = BaseContent.Fonts.Default.Small, Wrap = true };
        if (color != null) {
            label.TextColor = color.Value;
        }

        ((VerticalStackPanel) _combatLog.Content).AddChild(label);
        _combatLog.UpdateLayout();
        _combatLog.ScrollPosition = _combatLog.ScrollMaximum;
    }

    private void ClearCombatLog() {
        while (((VerticalStackPanel) _combatLog.Content).Widgets.Count > 0) {
            ((VerticalStackPanel) _combatLog.Content).Widgets.RemoveAt(0);
        }
    }
}