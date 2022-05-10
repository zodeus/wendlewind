using System;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class IdProvider : IExposable {
    private int _nextEntityId;
    private int _nextWorldObjectId;

    public int NextEntityId() {
        return NextId(ref _nextEntityId);
    }

    public int NextWorldObjectId() {
        return NextId(ref _nextWorldObjectId);
    }

    private static int NextId(ref int nextId) {
        if (Scribe.State is ScribeState.Saving or ScribeState.LoadingObjects) {
            throw new InvalidOperationException("Calling IdProvider.NextId during saving or loading. This is an error.");
        }

        int idToReturn = nextId;
        nextId++;
        if (nextId == int.MaxValue) {
            throw new InvalidOperationException("IdProvider.NextId reached int.MaxValue. This is an error.");
        }

        return idToReturn;
    }

    public void ExposeData() {
        Scribe_Values.Look(ref _nextEntityId, "NextEntityId");
        Scribe_Values.Look(ref _nextWorldObjectId, "NextWorldObjectId");
    }
}