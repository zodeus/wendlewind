using System;
using Grafted.Sim.Gui.DefWidgets;

namespace Grafted.Definitions;

public class Def {
    public string Moniker = "undefined";
    public string Label = "undefined";
    public string Description = "undefined";
    public ushort Index = ushort.MaxValue;
    public virtual Type DefUiClass => typeof(DefPanel);

    public override string ToString() {
        return Moniker;
    }

    public virtual void Initialize() {
        Log.Debug($"Initializing: {Moniker}");
    }

    public virtual void ResolveDependencies() {
        Log.Debug($"ResolveDependencies: {Moniker}");
    }

    public DefPanelBase UiPanelFor(Def def, DefPanelProperties? properties = null) {
        return (DefPanelBase) Activator.CreateInstance(DefUiClass, def, properties)!;
    }
}