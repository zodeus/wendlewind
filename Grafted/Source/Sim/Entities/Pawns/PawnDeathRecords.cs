using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PawnDeathRecords : IEnumerable<DeathRecord>, IExposable {
    private List<DeathRecord> _deathRecords = new();

    public IReadOnlyList<DeathRecord> List => _deathRecords;

    public PawnDeathRecords() { }

    public void RecordDeath(DeathRecord deathRecord) {
        _deathRecords.Add(deathRecord);
    }

    public IEnumerator<DeathRecord> GetEnumerator() {
        foreach (DeathRecord deathRecord in _deathRecords) {
            yield return deathRecord;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void ExposeData() {
        ScribeCollections.Look(ref _deathRecords!, "DeathRecords", LookMode.Deep);
    }
}

public class DeathRecord : IExposable {
    public string CauseOfDeath = "undefined";
    public string PawnName = "undefined";
    public int Round = -1;

    public void ExposeData() {
        ScribeValues.Look(ref CauseOfDeath!, "CauseOfDeath");
        ScribeValues.Look(ref PawnName!, "PawnName");
        ScribeValues.Look(ref Round!, "Round");
    }
}