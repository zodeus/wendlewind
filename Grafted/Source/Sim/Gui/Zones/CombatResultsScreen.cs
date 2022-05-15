using System.Linq;
using Grafted.Definitions;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.Widgets.EntityWidgets;
using Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.Widgets.MiscWidgets;
using Grafted.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;

namespace Grafted.Sim.Gui.Zones;

public class CombatResultsScreen : VerticalStackPanel {
    private readonly CombatEvent _combatEvent;
    private readonly PawnDetailPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private bool _autoLootEnabled = true;

    public CombatResultsScreen(AdventureGui gui, CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        _gameHud = new GameHud { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new PawnDetailPanel(Core.Sim.World.PlayerPawn, "Loot", _combatEvent.Loot) { Margin = new Thickness(0, 100, 0, 0) };

        Widget progressButton = GenerateProgressButton();
        progressButton.HorizontalAlignment = HorizontalAlignment.Center;

        Margin = new Thickness(0, 5, 0, 0);
        Spacing = 15;
        AddChild(_gameHud);
        AddChild(_pawnPanel);
        AddChild(progressButton);
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
        continueButton.Click += (_, _) => MoveToNextCombat();
        buttons.AddChild(continueButton);

        TextButton goHome = new(BaseContent.Styles.Button.Large) { Text = "Go Home" };
        goHome.Click += (_, _) => GoHome();
        buttons.AddChild(goHome);

        return new HorizontalStackPanel { Spacing = 10, Widgets = { DeathsButton(), buttons } };
    }

    private void GoHome() {
        if (_autoLootEnabled && DoAutoLoot() == false) {
            return;
        }

        Core.Sim.ChangeZone(Defs.Zones.VillageOfTheDamned);
    }

    private void MoveToNextCombat() {
        if (_autoLootEnabled && DoAutoLoot() == false) {
            return;
        }

        if (_combatEvent.Zone!.BossKilledThisRun) {
            GoHome();
            return;
        }

        if (_combatEvent.Zone.PercentTraveledThisRun < 1) {
            _combatEvent.Zone.Adventure!.Progress();
        }
    }

    private bool DoAutoLoot() {
        Pawn player = Core.Sim.World.PlayerPawn;
        var allItemsCollected = true;
        foreach (Item item in _combatEvent.Loot.ToList()) {
            if (player.Inventory.Entities.HasCapacityFor(item)) {
                player.Inventory.Entities.TryAdd(item);
            }
            else { allItemsCollected = false; }
        }

        if (allItemsCollected == false) {
            Core.Sim.Gui!.PushScreenMessage(new ScreenMessageData {
                Color = Color.Goldenrod, Duration = 2, Text = "There is remaining loot"
            });
            return false;
        }

        return true;
    }

    public void HandleInput() {
        if (Input.IsKeyPressed(Keys.Enter)) {
            MoveToNextCombat();
        }
    }

    public void Update() {
        _pawnPanel.Update();
        _gameHud.Update();
    }
}