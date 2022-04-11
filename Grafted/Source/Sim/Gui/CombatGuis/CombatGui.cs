using System;
using System.Linq;
using Grafted.Sim.Combat;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.CombatGuis;

public class CombatGui : SimulationGui {
    private readonly CombatEvent _combatEvent;
    private readonly ScrollViewer _combatLog;
    private readonly ProgramStatsPanel _programStats;
    private readonly Label _turnLabel;
    private readonly CombatPartyPanel _playerPartyPanel;
    private readonly CombatPartyPanel _opponentPartyPanel;
    private readonly CombatControlPanel _controlPanel;
    private readonly PawnBodyPanel _pawnBodyView;


    public CombatGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        _combatEvent.StateChangedAction += CombatStateChangedAction();
        _combatEvent.CombatRecord.LogMessageAddedAction += message => {
            AddCombatLogEntry(message.Text);
        };
        _programStats = new ProgramStatsPanel { GridRow = 0, GridColumn = 0, GridColumnSpan = 3, Margin = new Thickness(0, 10, 0, 10) };
        _playerPartyPanel = new CombatPartyPanel(_combatEvent, combatEvent.PlayerPawns, HorizontalAlignment.Right) {
            GridRow = 1, GridColumn = 0 /*, Width = 846*/
        };
        _opponentPartyPanel = new CombatPartyPanel(_combatEvent, combatEvent.EnemyPawns, HorizontalAlignment.Left) {
            GridRow = 1, GridColumn = 2 /*, Width = 846*/
        };
        _controlPanel = new CombatControlPanel(_combatEvent) {
            GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Center
        };
        _pawnBodyView = new PawnBodyPanel(combatEvent.PlayerPawns.First().Body, socket => {
            if (socket.AttachedPart != null) {
                ViewEntity(socket.AttachedPart);
            }
        }) {
            GridRow = 2, GridColumn = 0, HorizontalAlignment = HorizontalAlignment.Right,
            Width = 710,
            Margin = new Thickness(0, 0, 30, 0),
        };
        _combatLog = new ScrollViewer {
            Content = new VerticalStackPanel() { Padding = new Thickness(10) }
        };

        _turnLabel = new Label {
            Margin = new Thickness(0, 10, 0, 0),
            Font = BaseContent.Fonts.Fancy.Large, HorizontalAlignment = HorizontalAlignment.Center
        };

        VerticalStackPanel turnPane = new() {
            Width = 150,
            Height = 420,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Widgets = { _turnLabel }
        };

        VerticalStackPanel centerColumn = new() {
            Spacing = 0, GridRow = 1, GridColumn = 1, Widgets = {
                turnPane, _controlPanel
            }
        };

        HorizontalStackPanel logPanel = new() {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            GridRow = 2, GridColumn = 2,
            Height = 800,
            Width = 800,
            DefaultProportion = Proportion.Fill,
            Margin = new Thickness(30, 0, 30, 0),
            Padding = new Thickness(15),
            Widgets = {
                _combatLog
            }
        };

        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                _programStats,
                _playerPartyPanel, centerColumn, _opponentPartyPanel,
                _pawnBodyView, logPanel
            }
        };

        Desktop = new Desktop { Root = grid, HasExternalTextInput = true };
        //todo fairly certain there is an issue here, deregister this event when gui's change?
        Core.Instance.Window.TextInput += (_, a) => {
            Desktop.OnChar(a.Character);
        };
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

    public override void Render(SpriteBatch spriteBatch) {
        _programStats.Update();
        _playerPartyPanel.Update();
        _opponentPartyPanel.Update();
        _pawnBodyView.Update();
        MouseAttachment?.Update();
        _turnLabel.Text = $"Turn {_combatEvent.CurrentTurnNum.ToString().PadLeft(2, '0')}";
        Desktop.Render();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        MouseAttachment?.Render(spriteBatch);
        spriteBatch.End();
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