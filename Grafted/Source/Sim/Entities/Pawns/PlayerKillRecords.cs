using System.Collections;

namespace Grafted.Sim.Entities.Pawns;

public class PlayerKillRecords : IEnumerable<DeathRecord>, IExposable
{
    private int _currentRound = 1;
    private List<DeathRecord> _deathRecords = new();

    public IReadOnlyList<DeathRecord> List => _deathRecords;

    public void RecordDeath(DeathRecord deathRecord)
    {
        deathRecord.Round = _currentRound++;
        _deathRecords.Add(deathRecord);
    }

    public void Reset()
    {
        _currentRound = 1;
        _deathRecords = [];
    }

    public IEnumerator<DeathRecord> GetEnumerator()
    {
        foreach (DeathRecord deathRecord in _deathRecords)
        {
            yield return deathRecord;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _deathRecords!, "DeathRecords", LookMode.Deep);
        ScribeValues.Look(ref _currentRound, "CurrentRound");
    }
}

public class DeathRecord : IExposable
{
    public string CauseOfDeath = "undefined";
    public double TotalDamageDealt;
    public int Ticks;
    public ZoneDef ZoneDef = null!;
    public string PawnName = "undefined";
    public int Round = -1;

    public void ExposeData()
    {
        ScribeValues.Look(ref TotalDamageDealt!, "TotalDamageDealt");
        ScribeValues.Look(ref CauseOfDeath!, "CauseOfDeath");
        ScribeValues.Look(ref Ticks, "Ticks");
        ScribeValues.Look(ref PawnName!, "PawnName");
        ScribeValues.Look(ref Round, "Round");
        ScribeDefs.Look(ref ZoneDef!, "ZoneDef");
    }
}

public struct DeathEvent
{
    public DeathRecord Record;
    public Pawn Pawn;
}