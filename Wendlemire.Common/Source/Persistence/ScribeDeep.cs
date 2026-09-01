namespace Wendlemire.Sim.Persistence;

public class ScribeDeep {
    public static void Look<T>(ref T? target, string label, params object[] ctorArgs) {
        switch (Scribe.State) {
            case ScribeState.Saving: {
                if (typeof(IExposable).IsAssignableFrom(typeof(T))==false) {
                    Log.Error("Cannot use LookDeep to save non-IExposable non-null " + label + " of type " + typeof(T));
                    return;
                }

                var exposable = (IExposable) target!;
                if (target == null) {
                    if (Scribe.EnterNode(label)) {
                        try {
                            Scribe.Saver.WriteAttribute("IsNull", "True");
                        }
                        finally {
                            Scribe.ExitNode();
                        }
                    }
                }
                else if (Scribe.EnterNode(label)) {
                    try {
                        if (target.GetType() != typeof(T) || typeof(T).IsGenericTypeDefinition) {
                            Scribe.Saver.WriteAttribute("Class", GenTypes.GetTypeNameWithoutIgnoredNamespaces(target.GetType())!);
                        }

                        exposable.ExposeData();
                    }
                    catch (OutOfMemoryException) {
                        throw;
                    }
                    catch (Exception e) {
                        Log.Error("Exception while saving " + exposable + ": " + e);
                    }
                    finally {
                        Scribe.ExitNode();
                    }
                }

                //Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(target, label);
                break;
            }
            case ScribeState.LoadingObjects:
                try {
                    target = ScribeExtractor.SaveableFromNode<T>(Scribe.Loader.CurXmlParent?[label], ctorArgs)!;
                }
                catch (Exception ex3) {
                    Log.Error("Exception while loading " + Scribe.Loader.CurXmlParent?[label] + ": " + ex3);
                    target = default!;
                }

                break;
        }
    }
}