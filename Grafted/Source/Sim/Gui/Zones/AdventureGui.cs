using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Zones;
using Grafted.Sim.Zones.Handlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Zones;

public class AdventureGui : ZoneGui {
    private PawnBodyEffectsWindow _pawnBodyEffectsWindow;
    private CombatScreen? _combatScreen;
    private CombatResultsScreen? _combatResultsScreen;

    public override void Initialize(Zone zone) {

        Desktop = new Desktop {
            Root = new Panel(),
            HasExternalTextInput = true
        };

        _pawnBodyEffectsWindow = new PawnBodyEffectsWindow(Core.Sim.World.PlayerPawn);
        _pawnBodyEffectsWindow.Show(Desktop, new Point(50, 20));

        base.Initialize(zone);
    }

    public override void Update(float deltaTime) {
        if (_combatScreen == null && Zone.Adventure?.State == AdventureState.InCombat) {
            _combatResultsScreen?.RemoveFromParent();
            _combatResultsScreen = null;
            _combatScreen = new CombatScreen(this, Zone.Adventure?.ActiveCombat!);
            (Desktop.Root as Panel)!.AddChild(_combatScreen);
        }

        if (_combatResultsScreen == null && Zone.Adventure?.State == AdventureState.CombatResults) {
            _combatScreen?.RemoveFromParent();
            _combatScreen = null;
            _combatResultsScreen = new CombatResultsScreen(this, Zone.Adventure?.ActiveCombat!);
            (Desktop.Root as Panel)!.AddChild(_combatResultsScreen);
        }

        _combatScreen?.Update();
        _combatResultsScreen?.Update();
        _pawnBodyEffectsWindow.Update();
        base.Update(deltaTime);
    }

    public override void HandleInput() {
        _combatResultsScreen?.HandleInput();
        base.HandleInput();
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {
        base.Render(spriteBatch, deltaTime);
        //todo I think maybe this is where we would render pawns?... sigh
    }
}