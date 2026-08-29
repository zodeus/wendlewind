namespace Wendlewind.Sim.Persistence;

public enum ScribeState : byte {
    Inactive,
    Saving,
    LoadingObjects,
    ResolvingCrossReferences,
    PostLoadInitialization
}

public enum LookMode : byte {
    Undefined,
    Value,
    Deep,
    Reference,
    Def
}

public static class Scribe {
    public static readonly ScribeSaver Saver = new ScribeSaver();

    public static readonly ScribeLoader Loader = new ScribeLoader();

    public static ScribeState State = ScribeState.Inactive;

    /// <summary>
    /// Optional factory used when reconstructing <see cref="IExposable"/> sim objects.
    /// Defs still use Activator in <c>DirectXmlToObject</c>.
    /// </summary>
    public static IScribeObjectFactory? ObjectFactory { get; set; }

    public static void ForceStop() {
        State = ScribeState.Inactive;
        Saver.ForceStop();
        Loader.ForceStop();
    }

    public static bool EnterNode(string nodeName) {
        switch (State) {
            case ScribeState.Inactive:
                return false;
            case ScribeState.Saving:
                return Saver.EnterNode(nodeName);
            case ScribeState.LoadingObjects:
            case ScribeState.ResolvingCrossReferences:
            case ScribeState.PostLoadInitialization:
                return Loader.EnterNode(nodeName);
            default:
                return true;
        }
    }

    public static void ExitNode() {
        if (State == ScribeState.Saving) {
            Saver.ExitNode();
        }

        if (State == ScribeState.LoadingObjects || State == ScribeState.ResolvingCrossReferences || State == ScribeState.PostLoadInitialization) {
            Loader.ExitNode();
        }
    }
}