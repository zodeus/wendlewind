namespace Grafted.Sim.Persistence;

public class CrossRefHandler {
    private readonly LoadedObjectDirectory _loadedObjectDirectory = new();

    public readonly LoadIDsWantedBank LoadIDs = new();

    public readonly List<IExposable> CrossReferencingExposables = new();

    public void RegisterForCrossRefResolve(IExposable? exposable) {
        if (Scribe.State != ScribeState.LoadingObjects) {
            Log.Error(string.Concat("Registered ", exposable, " for cross ref resolve, but current mode is ", Scribe.State));
        }
        else if (exposable != null) {
            CrossReferencingExposables.Add(exposable);
        }
    }

    public void ResolveAllCrossReferences() {
        Scribe.State = ScribeState.ResolvingCrossReferences;
        Log.Debug("==================Register the saveables all so we can find them later");

        foreach (var crossReferencingExposable in CrossReferencingExposables) {
            if (crossReferencingExposable is IIdentityProvider identityProvider) {
                _loadedObjectDirectory.RegisterLoaded(identityProvider);
            }
        }

        Log.Debug("==================Fill all cross-references to the saveables");

        foreach (var crossReferencingExposable2 in CrossReferencingExposables) {
            try {
                Scribe.Loader.CurParent = crossReferencingExposable2;
                Scribe.Loader.CurPathRelToParent = null;
                crossReferencingExposable2.ExposeData();
            }
            catch (Exception arg) {
                Log.Error("Could not resolve cross refs: " + arg);
            }
        }

        Scribe.Loader.CurParent = null;
        Scribe.Loader.CurPathRelToParent = null;
        Scribe.State = ScribeState.Inactive;
        Clear(true);
    }

    public T? TakeResolvedRef<T>(string pathRelToParent, IExposable parent) where T : IIdentityProvider {
        var loadId = LoadIDs.Take<T>(pathRelToParent, parent)!;
        return _loadedObjectDirectory.ObjectWithLoadId<T>(loadId);
    }

    public T? TakeResolvedRef<T>(string toAppendToPathRelToParent) where T : IIdentityProvider {
        var text = Scribe.Loader.CurPathRelToParent!;
        if (!toAppendToPathRelToParent.NullOrEmpty()) {
            text = text + "/" + toAppendToPathRelToParent;
        }

        return TakeResolvedRef<T>(text, Scribe.Loader.CurParent!);
    }

    public List<T> TakeResolvedRefList<T>(string pathRelToParent, IExposable parent) {
        List<string> list = LoadIDs.TakeList(pathRelToParent, parent);
        List<T> list2 = new();
        for (var i = 0; i < list.Count; i++) {
            list2.Add(_loadedObjectDirectory.ObjectWithLoadId<T>(list[i])!);
        }

        return list2;
    }

    public List<T> TakeResolvedRefList<T>(string toAppendToPathRelToParent) {
        var text = Scribe.Loader.CurPathRelToParent!;
        if (!toAppendToPathRelToParent.NullOrEmpty()) {
            text = text + "/" + toAppendToPathRelToParent;
        }

        return TakeResolvedRefList<T>(text, Scribe.Loader.CurParent!);
    }

    public void Clear(bool errorIfNotEmpty) {
        if (errorIfNotEmpty) {
            LoadIDs.ConfirmClear();
        }
        else {
            LoadIDs.Clear();
        }

        CrossReferencingExposables.Clear();
        _loadedObjectDirectory.Clear();
    }
}