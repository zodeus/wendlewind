namespace Grafted.Sim.Persistence;

public class LoadedObjectDirectory {
    private Dictionary<string, IIdentityProvider> allObjectsByLoadID = new();

    public void Clear() {
        allObjectsByLoadID.Clear();
    }

    public void RegisterLoaded(IIdentityProvider reffable) {
        // if (Prefs.DevMode) {
        string text = "[excepted]";
        try {
            text = reffable.GetUniqueId();
        }
        catch (Exception) {
            //ignored 
        }

        string text2 = $"[excepted: casting {reffable.GetType().Name} to string]";
        try {
            text2 = reffable.ToString()!;
        }
        catch (Exception) {
            //      ignored
        }

        if (allObjectsByLoadID.TryGetValue(text, out IIdentityProvider? value)) {
            string text3 = "";
            Log.Error(string.Concat("Cannot register ", reffable.GetType(), " ", text2, ", (id=", text, " in loaded object directory. Id already used by ", value.GetType(), " ",
                value.ToString(), ".", text3));
            return;
        }
        // }

        try {
            allObjectsByLoadID.Add(reffable.GetUniqueId(), reffable);
        }
        catch (Exception ex5) {
            string text4 = "[excepted]";
            try {
                text4 = reffable.GetUniqueId();
            }
            catch (Exception) {
                // ignored
            }

            string text5 = "[excepted]";
            try {
                text5 = reffable.ToString()!;
            }
            catch (Exception) {
                // ignored
            }

            Log.Error(string.Concat("Exception registering ", reffable.GetType(), " ", text5, " in loaded object directory with unique load ID ", text4, ": ", ex5));
        }
    }

    public T? ObjectWithLoadId<T>(string loadId) {
        if (loadId.NullOrEmpty() || loadId == "null") {
            return default;
        }

        if (allObjectsByLoadID.TryGetValue(loadId, out IIdentityProvider? value)) {
            try {
                return (T) value;
            }
            catch (Exception ex) {
                Log.Error(string.Concat("Exception getting object with load id ", loadId, " of type ", typeof(T), ". What we loaded was ", value.ToString(), ". Exception:\n", ex));
                return default;
            }
        }

        Log.Warning(string.Concat("Could not resolve reference to object with loadID ", loadId, " of type ", typeof(T),
            ". Was it compressed away, destroyed, had no ID number, or not saved/loaded right? curParent=", Scribe.Loader.CurParent?.ToString(), " curPathRelToParent=",
            Scribe.Loader.CurPathRelToParent));
        return default;
    }
}