namespace Grafted.Sim;

public class IdProvider : IExposable {
    private int _nextEntityId;
    private int _nextBodyPartModifierId;

    public int NextEntityId() {
        return NextId(ref _nextEntityId);
    }

    public int NextBodyPartModifierId() {
        return NextId(ref _nextBodyPartModifierId);
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
        ScribeValues.Look(ref _nextEntityId, "NextEntityId");
        ScribeValues.Look(ref _nextBodyPartModifierId, "NextBodyPartModifierId");
    }
}