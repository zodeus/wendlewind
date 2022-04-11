using System.Collections.Generic;

namespace Grafted;

public enum CoreEvent {
    GraphicsDeviceReset,
    SceneChanged,
    OrientationChanged,
    Exiting
}

/// <summary>
/// comparer that should be passed to a dictionary constructor to avoid boxing/unboxing when using an enum as a key on Mono
/// </summary>
public struct CoreEventComparer : IEqualityComparer<CoreEvent> {
    public bool Equals(CoreEvent x, CoreEvent y) {
        return x == y;
    }

    public int GetHashCode(CoreEvent obj) {
        return (int) obj;
    }
}