using Grafted.Sim.SpecialEvents;

namespace Grafted.Sim.Gui.SpecialEvents;

public abstract class SpecialEventGui : BaseGui {
    protected readonly SpecialEventHandler Handler;

    public SpecialEventGui(SpecialEventHandler handler) {
        Handler = handler;
    }
}