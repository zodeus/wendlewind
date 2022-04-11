using System;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class IdProvider : IExposable {
    private int _nextEntityId;
    private int _nextWorldObjectId;
    private int _nextMapId;
    private int _nextJobId;
    private int _nextHealthConditionId;

    public int NextEntityId() {
        return NextId(ref _nextEntityId);
    }

    public int NextWorldObjectId() {
        return NextId(ref _nextWorldObjectId);
    }

    public int NextMapId() {
        return NextId(ref _nextMapId);
    }

    public int NextJobId() {
        return NextId(ref _nextJobId);
    }

    public int NextHealthConditionId() {
        return NextId(ref _nextHealthConditionId);
    }

    private static int NextId(ref int nextId) {
        /*if (Scribe.State is ScribeState.Saving or ScribeState.LoadingObjects) {
            throw new InvalidOperationException("Calling IdProvider.NextId during saving or loading. This is an error.");
        }*/

        int idToReturn = nextId;
        nextId++;
        if (nextId == int.MaxValue) {
            throw new InvalidOperationException("IdProvider.NextId reached int.MaxValue. This is an error.");
        }

        return idToReturn;
    }

    public void ExposeData() {
        /*Scribe_Values.Look(ref _nextEntityId, "NextEntityId");
        Scribe_Values.Look(ref _nextWorldObjectId, "NextWorldObjectId");
        Scribe_Values.Look(ref _nextMapId, "NextMapId");
        Scribe_Values.Look(ref _nextJobId, "NextJobId");
        Scribe_Values.Look(ref _nextHealthConditionId, "NextHealthConditionId");*/
    }
}