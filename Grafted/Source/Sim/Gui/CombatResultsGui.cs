using System.Linq;
using Grafted.Debug;
using Grafted.Definitions;
using Grafted.Sim.Combat;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.Sim.Gui.TownWidgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;

namespace Grafted.Sim.Gui;

public class CombatResultsGui : BaseGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnDetailPanel _pawnPanel;
    private readonly GameStatsPanel _gameStatsPanel;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        _gameStatsPanel = new GameStatsPanel { HorizontalAlignment = HorizontalAlignment.Center };
        _pawnPanel = new PawnDetailPanel(Core.Sim.World.PlayerPawns[0], "Loot", _combatEvent.Loot);

        Widget progressButton = GenerateProgressButton();
        progressButton.HorizontalAlignment = HorizontalAlignment.Right;

        VerticalStackPanel panel = new() {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0), Spacing = 15,
            Widgets = {
                _gameStatsPanel,
                _pawnPanel,
                progressButton
            }
        };

        Desktop = new Desktop { Root = panel, HasExternalTextInput = true };
    }

    private Widget DeathsButton() {
        ImageButton image = new(BaseContent.Styles.Button.Large) {
            Image = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Skull], Width = 32, Height = 32,
            Padding = new Thickness(10)
        };
        image.TouchDown += (_, _) => {
            new PawnDeathRecordsWindow(Core.Sim.World.DeathRecords).Show(Desktop);
        };
        return image;
    }

    private Widget GenerateProgressButton() {
        HorizontalStackPanel buttons = new() {
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        TextButton continueButton = new(BaseContent.Styles.Button.Large) { Text = "Carry on" };
        continueButton.Click += (_, _) => {
            if (Core.Sim.World.CurrentZone.Def == Defs.Zones.Intro) {
                HandleIntro();
                return;
            }

            Core.Sim.World.DoZoneTravel();
            if (Core.Sim.World.PlayerPawns[0].IsDead) {
                ShowDeathWindow();
                return;
            }

            Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
        };
        buttons.AddChild(continueButton);
        if (Core.Sim.World.CurrentZone.Def != Defs.Zones.Intro) {
            TextButton goHome = new(BaseContent.Styles.Button.Large) { Text = "Go Home" };
            goHome.Click += (_, _) => {
                Core.Sim.World.MoveToZone(Defs.Zones.VillageOfTheDamned);
                Core.Sim.Gui = new TownGui(Core.Sim.World.CurrentZone.Town!);
            };
            buttons.AddChild(goHome);
        }

        return new HorizontalStackPanel { Spacing = 10, Widgets = { DeathsButton(), buttons } };
    }

    private void HandleIntro() {
        if (Core.Sim.World.TotalKills < 15) {
            Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
            return;
        }

        if (DebugSettings.SkipIntroDialogue) {
            Core.Sim.World.MoveToZone(Defs.Zones.VillageOfTheDamned);
            Core.Sim.Gui = new TownGui(Core.Sim.World.CurrentZone.Town!);
        }
        else {
            Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue());
        }
    }

    public override void Update(float deltaTime) {
        _pawnPanel.Update();
        _gameStatsPanel.Update();
        base.Update(deltaTime);
    }
}