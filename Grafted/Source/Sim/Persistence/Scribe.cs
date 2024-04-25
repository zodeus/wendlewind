namespace Grafted.Sim.Persistence;

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
    public static readonly ScribeSaver Saver = new();

    public static readonly ScribeLoader Loader = new();

    public static ScribeState State = ScribeState.Inactive;

    public static void ForceStop() {
        State = ScribeState.Inactive;
        Saver.ForceStop();
        Loader.ForceStop();
    }

    public static bool EnterNode(string nodeName)
    {
        return State switch
        {
            ScribeState.Inactive => false,
            ScribeState.Saving => Saver.EnterNode(nodeName),
            ScribeState.LoadingObjects or ScribeState.ResolvingCrossReferences or ScribeState.PostLoadInitialization => Loader.EnterNode(nodeName),
            _ => true
        };
    }

    public static void ExitNode()
    {
        switch (State)
        {
            case ScribeState.Saving:
                Saver.ExitNode();
                break;
            case ScribeState.LoadingObjects or ScribeState.ResolvingCrossReferences or ScribeState.PostLoadInitialization:
                Loader.ExitNode();
                break;
        }
    }
}