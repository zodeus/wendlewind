using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.MiscWidgets;

namespace Grafted.Sim.Gui.TownWidgets;

public class HomePanel : TabPanel, IUpdatable {
    public HomePanel(Pawn pawn, Town town) {

        AddTab("Summary", new TownSummaryPanel());
        AddTab("Character", new PawnDetailPanel(pawn, "Storage", town.Storage));
        //AddTab("Upgrades", new Label { Text = "Coming some day..." });
        //AddTab("Cooking", new Label { Text = "Coming some day..." });
    }
}