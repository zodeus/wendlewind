using System;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.Sim.Gui.TownWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;
using Label = Myra.Graphics2D.UI.Label;

namespace Grafted.Sim.Gui;

public class CombatResultsGui : BaseGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnDetailPanel _pawnPanel;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        Label title = new(BaseContent.Styles.Label.Large) {
            GridRow = 0, GridColumn = 0,
            Text = $"Total kills \\c[{CombatSequence.TextColorGreen}]{Core.Sim.World.TotalKills}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _pawnPanel = new PawnDetailPanel(Core.Sim.World.PlayerPawns[0], "Loot", _combatEvent.Loot) {
            GridRow = 1, GridColumn = 0
        };

        Widget progressButton = GenerateProgressButton();
        progressButton.GridRow = 2;
        progressButton.GridColumn = 0;
        progressButton.HorizontalAlignment = HorizontalAlignment.Right;

        Grid grid = new() {
            ShowGridLines = false, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(50),
            Margin = new Thickness(0, 50, 0, 0), GridLinesColor = Color.Red, RowSpacing = 20,
            DefaultRowProportion = Proportion.Auto, DefaultColumnProportion = Proportion.Auto,
            Widgets = {
                title,
                _pawnPanel,
                progressButton
            }
        };

        Desktop = new Desktop { Root = grid, HasExternalTextInput = true };
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
        if (_combatEvent.PlayerPawns.First().IsDead) {
            TextButton button = new(BaseContent.Styles.Button.Large) {
                Text = "You dead son"
            };
            button.Click += (_, _) => ((GameScene) Core.Scene.ActiveScene!).QuickPlay();
            buttons.AddChild(button);
        }
        else {
            TextButton continueButton = new(BaseContent.Styles.Button.Large) {
                Text = "Carry on"
            };
            continueButton.Click += (_, _) => {
                if (Core.Sim.World.CurrentZone.Def == Defs.Zones.Intro) {
                    HandleIntro();
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

        }

        return new HorizontalStackPanel { Spacing = 10, Widgets = { DeathsButton(), buttons } };
    }

    private void HandleIntro() {
        if (Core.Sim.World.TotalKills < 15) {
            Core.Sim.Gui = new CombatGui(Core.Sim.World.NextCombat());
            return;
        }

        Core.Sim.Gui = new DialogueGui(Core.Sim.World.NextDialogue());
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {
        _pawnPanel.Update();
        base.Render(spriteBatch, deltaTime);
    }
}