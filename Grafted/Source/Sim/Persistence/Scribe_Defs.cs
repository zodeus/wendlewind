using Grafted.Definitions;

namespace Grafted.Sim.Persistence;

public static class Scribe_Defs {
    public static void Look<T>(ref T? value, string label) where T : Def, new() {
        if (Scribe.State == ScribeState.Saving) {
            string? value2 = (value != null) ? value.Moniker : "null";
            Scribe_Values.Look(ref value2, label, "null");
        }
        else if (Scribe.State == ScribeState.LoadingObjects) {
            value = ScribeExtractor.DefFromNode<T>(Scribe.Loader.CurXmlParent?[label]!);
        }
    }
}