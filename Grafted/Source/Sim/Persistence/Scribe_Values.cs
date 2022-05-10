using Grafted.Definitions;
using Grafted.Sim.Entities;

namespace Grafted.Sim.Persistence;

public static class Scribe_Values {
    public static void Look<T>(ref T? value, string label, T? defaultValue = default, bool forceSave = false) {
        if (Scribe.State == ScribeState.Saving) {
            /*if (typeof(T) == typeof(TargetInfo)) {
                Log.Error("Saving a TargetInfo " + label + " with Scribe_Values. TargetInfos must be saved with Scribe_TargetInfo.");
            }
            else*/
            if (typeof(Entity).IsAssignableFrom(typeof(T))) {
                Log.Error($"{typeof(T)}: Using Scribe_Values with a Entity reference " + label + ". Use Scribe_References or Scribe_Deep instead.");
            }
            else if (typeof(IExposable).IsAssignableFrom(typeof(T))) {
                Log.Error($"{typeof(T)}: Using Scribe_Values with a IExposable reference " + label + ". Use Scribe_References or Scribe_Deep instead.");
            }
            else if (typeof(Def).IsAssignableFrom(typeof(T))) {
                Log.Error($"{typeof(T)}: Using Scribe_Values with a Def " + label + ". Use Scribe_Defs instead.");
            }
            else {
                if (!forceSave && (value != null || defaultValue == null) && (value == null || value.Equals(defaultValue))) {
                    return;
                }

                if (value == null) {
                    if (Scribe.EnterNode(label)) {
                        try {
                            Scribe.Saver.WriteAttribute("IsNull", "True");
                        }
                        finally {
                            Scribe.ExitNode();
                        }
                    }
                }
                else {
                    Scribe.Saver.WriteElement(label, value.ToString());
                }
            }
        }
        else if (Scribe.State == ScribeState.LoadingObjects) {
            value = ScribeExtractor.ValueFromNode(Scribe.Loader.CurXmlParent?[label], defaultValue);
        }
    }
}