namespace Grafted.Sim.Persistence;

public class PostLoadInitializer {
    private readonly HashSet<IExposable> _saveablesToPostLoad = new();

    public void RegisterForPostLoadInit(IExposable? s) {
        if (Scribe.State != ScribeState.LoadingObjects) {
            Log.Error(string.Concat("Registered ", s, " for post load init, but current mode is ", Scribe.State));
        }
        else if (s == null) {
            Log.Warning("Trying to register null in RegisterForPostLoadInit.");
        }
        else if (_saveablesToPostLoad.Contains(s)) {
            Log.Warning("Tried to register in RegisterForPostLoadInit when already registered: " + s);
        }
        else {
            _saveablesToPostLoad.Add(s);
        }
    }

    public void DoAllPostLoadInits() {
        Scribe.State = ScribeState.PostLoadInitialization;
        foreach (IExposable item in _saveablesToPostLoad) {
            try {
                Scribe.Loader.CurParent = item;
                Scribe.Loader.CurPathRelToParent = null;
                item.ExposeData();
            }
            catch (Exception ex) {
                Log.Error("Could not do PostLoadInit on " + item + ": " + ex);
            }
        }

        Clear();
        Scribe.Loader.CurParent = null;
        Scribe.Loader.CurPathRelToParent = null;
        Scribe.State = ScribeState.Inactive;
    }

    public void Clear() {
        _saveablesToPostLoad.Clear();
    }
}