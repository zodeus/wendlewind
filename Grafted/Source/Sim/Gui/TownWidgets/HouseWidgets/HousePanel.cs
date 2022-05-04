using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui.EntityWidgets.PawnWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.TownWidgets.HouseWidgets;

public class HousePanel : TabPanel, IUpdatable {
    public HousePanel(Pawn pawn, Town town) {

        AddTab("General", new HouseGeneralPanel(town));
        AddTab("Character", new PawnDetailPanel(pawn, "Storage", town.GetStructure<TownStructureHouse>()!.Storage));
    }
}