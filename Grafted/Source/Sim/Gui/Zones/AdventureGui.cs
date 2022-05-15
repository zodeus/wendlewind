using Grafted.Sim.Zones.Handlers;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.Zones;

public class AdventureGui : ZoneGui {
    public AdventureGui() {
        Desktop = new Desktop {
            Root = new VerticalStackPanel { },
            HasExternalTextInput = true
        };
    }

    public override void Update(float deltaTime) {
        if (Desktop.Root is not CombatScreen && Zone.Adventure?.State == AdventureState.InCombat) {
            Desktop.Root = new CombatScreen(this, Zone.Adventure?.ActiveCombat!);
        }

        if (Desktop.Root is not CombatResultsScreen && Zone.Adventure?.State == AdventureState.CombatResults) {
            Desktop.Root = new CombatResultsScreen(this, Zone.Adventure?.ActiveCombat!);
        }

        (Desktop.Root as CombatScreen)?.Update();
        (Desktop.Root as CombatResultsScreen)?.Update();
        base.Update(deltaTime);
    }

    public override void HandleInput() {
        (Desktop.Root as CombatResultsScreen)?.HandleInput();
    }

    public override void Render(SpriteBatch spriteBatch, float deltaTime) {


        base.Render(spriteBatch, deltaTime);
    }
}