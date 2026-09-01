namespace Wendlemire.Sim.Persistence;

public static class ScribeDefs {
    public static void Look<T>(ref T? value, string label) where T : Def, new() {
        if (Scribe.State == ScribeState.Saving) {
            var value2 = (value != null) ? value.Moniker : "null";
            ScribeValues.Look(ref value2, label, "null");
        }
        else if (Scribe.State == ScribeState.LoadingObjects) {
            value = ScribeExtractor.DefFromNode<T>(Scribe.Loader.CurXmlParent?[label]!);
        }
    }
}