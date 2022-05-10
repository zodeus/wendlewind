namespace Grafted.Sim.Persistence;

public static class Scribe_References {
    public static void Look<T>(ref T? referenceObject, string label) where T : IIdentityProvider {
        switch (Scribe.State) {
            case ScribeState.Saving when referenceObject == null:
                Scribe.Saver.WriteElement(label, "null");
                return;
            case ScribeState.Saving: {
                //todo not sure whats going on here
                //Entity? entity = referenceObject as Entity;
                //if (entity == null) {
                string uniqueLoadId = referenceObject.GetUniqueId();
                Scribe.Saver.WriteElement(label, uniqueLoadId);
                //}
                //else {
                //Log.Error("I think we skipped saving entities");
                //}

                break;
            }
            case ScribeState.LoadingObjects: {
                if (Scribe.Loader.CurParent != null && Scribe.Loader.CurParent.GetType().IsValueType) {
                    Log.Warning(string.Concat("Trying to load reference of an object of type ", typeof(T), " with label ", label,
                        ", but our current node is a value type. The reference won't be loaded properly. curParent=", Scribe.Loader.CurParent));
                }

                string targetLoadId = Scribe.Loader.CurXmlParent?[label]?.InnerText ?? string.Empty;
                Scribe.Loader.CrossRefs.LoadIDs.RegisterLoadIdReadFromXml(targetLoadId, typeof(T), label);
                break;
            }
            case ScribeState.ResolvingCrossReferences:
                referenceObject = Scribe.Loader.CrossRefs.TakeResolvedRef<T>(label);
                break;
        }
    }
}