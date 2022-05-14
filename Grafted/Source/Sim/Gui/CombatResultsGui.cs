using System.Linq;
using Grafted.Debug;
using Grafted.Definitions;
using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.Widgets.EntityWidgets;
using Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.Widgets.MiscWidgets;
using Grafted.UI;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment;

namespace Grafted.Sim.Gui;

public class CombatResultsGui : BaseGui {
    private readonly CombatEvent _combatEvent;
    private readonly PawnDetailPanel _pawnPanel;
    private readonly GameHud _gameHud;
    private bool _autoLootEnabled = true;
    private readonly PawnBodyEffectsWindow _pawnBodyEffectsWindow;

    public CombatResultsGui(CombatEvent combatEvent) {
        _combatEvent = combatEvent;
        _gameHud = new GameHud { HorizontalAlignment = HorizontalAlignment.Stretch };
        _pawnPanel = new PawnDetailPanel(Core.Sim.World.PlayerPawns[0], "Loot", _combatEvent.Loot) { Margin = new Thickness(0, 100, 0, 0) };

        Widget progressButton = GenerateProgressButton();
        progressButton.HorizontalAlignment = HorizontalAlignment.Center;

        VerticalStackPanel panel = new() {
            Margin = new Thickness(0, 5, 0, 0), Spacing = 15,
            Widgets = {
                _gameHud,
                _pawnPanel,
                progressButton
            }
        };

        Desktop = new Desktop { Root = panel, HasExternalTextInput = true };

        _pawnBodyEffectsWindow = new PawnBodyEffectsWindow(Core.Sim.World.PlayerPawn);
        _pawnBodyEffectsWindow.Show(Desktop, new Point(50, 20));
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

        Core.Sim.World.MoveToZone(Defs.Zones.VillageOfTheDamned);
        Core.Sim.Gui = new TownGui(Core.Sim.World.CurrentZone.Town!);
    }

    private void MoveToNextCombat() {
        if (_autoLootEnabled && DoAutoLoot() == false) {
            return;
        }

        if (Core.Sim.World.CurrentZone!.BossKilledThisRun) {
            GoHome();
            return;
        }

        if (Core.Sim.World.CurrentZone!.PercentTraveled < 1) {
            Core.Sim.World.DoZoneTravel();
        }

        if (Core.Sim.World.PlayerPawns[0].IsDead) {
            ShowDeathWindow();
            return;
        }

        Core.Sim.ActivateCombatEvent(Core.Sim.World.NextCombat());
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

    public override void HandleInput() {
        base.HandleInput();
        if (Input.IsKeyPressed(Keys.Enter)) {
            MoveToNextCombat();
        }
    }

    public override void Update(float deltaTime) {
        _pawnPanel.Update();
        _gameHud.Update();
        _pawnBodyEffectsWindow.Update();
        base.Update(deltaTime);
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        spriteBatch.Draw(Core.Sim.World.CurrentZone.Def.BackgroundTexture, new Rectangle(0, 0, Screen.Width, Screen.Height), new Color(255, 255, 255, Core.Sim.World.CurrentZone.Def.BackgroundTextureTransparency));
        spriteBatch.End();
        base.Render(spriteBatch, deltaTime);
    }
}