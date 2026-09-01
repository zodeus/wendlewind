using System.Text.Json;
using Wendlewind.NetCode.Contracts;

namespace Wendlewind.NetCode;

public sealed class BuildPool
{
    private readonly Dictionary<int, List<BuildSnapshot>> _rounds = new();
    private readonly string? _path;
    private readonly object _gate = new();

    public BuildPool(string? persistPath = null)
    {
        _path = persistPath;
        Load();
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _rounds.Values.Sum(list => list.Count);
            }
        }
    }

    public AdminPoolState Snapshot()
    {
        lock (_gate)
        {
            var builds = _rounds
                .OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value)
                .OrderByDescending(build => build.SubmittedAt ?? DateTimeOffset.MinValue)
                .ToList();
            return new AdminPoolState
            {
                Count = builds.Count,
                Rounds = _rounds
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new AdminPoolRound { Round = pair.Key, Builds = pair.Value.Count })
                    .ToList(),
                Builds = builds
            };
        }
    }

    public void Upsert(BuildSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.PlayerId))
        {
            throw new ArgumentException("BuildSnapshot.PlayerId is required.", nameof(snapshot));
        }

        var stored = snapshot with
        {
            SubmittedAt = snapshot.SubmittedAt ?? DateTimeOffset.UtcNow,
            Round = ResolveRound(snapshot)
        };

        lock (_gate)
        {
            GetRoundList(stored.Round).Add(stored);
            PersistUnlocked();
        }
    }

    public int RemovePlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return 0;
        }

        lock (_gate)
        {
            var removed = 0;
            foreach (var list in _rounds.Values)
            {
                removed += list.RemoveAll(build => IsSamePlayer(build.PlayerId, playerId));
            }

            if (removed > 0)
            {
                PersistUnlocked();
            }

            return removed;
        }
    }

    public BuildSnapshot? Get(string playerId)
    {
        lock (_gate)
        {
            return _rounds.Values
                .SelectMany(list => list)
                .Where(b => string.Equals(b.PlayerId, playerId, StringComparison.Ordinal))
                .OrderByDescending(b => b.SubmittedAt ?? DateTimeOffset.MinValue)
                .FirstOrDefault();
        }
    }

    public BuildSnapshot? PickOpponent(int round, string? excludePlayerId = null, int attackerRating = 0)
    {
        lock (_gate)
        {
            var others = Candidates(round, excludePlayerId);
            if (others.Count > 0)
            {
                return PickRated(others, attackerRating);
            }

            if (!string.IsNullOrWhiteSpace(excludePlayerId))
            {
                foreach (var otherRound in _rounds.Keys.OrderBy(r => Math.Abs(r - round)).ThenByDescending(r => r))
                {
                    if (otherRound == round)
                    {
                        continue;
                    }

                    var fallback = Candidates(otherRound, excludePlayerId);
                    if (fallback.Count > 0)
                    {
                        return PickRated(fallback, attackerRating);
                    }
                }
            }

            var ownRound = Candidates(round, excludePlayerId: null);
            return ownRound.Count > 0 ? PickRated(ownRound, attackerRating) : null;
        }
    }

    private List<BuildSnapshot> Candidates(int round, string? excludePlayerId)
    {
        if (!_rounds.TryGetValue(round, out var bucket) || bucket.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(excludePlayerId))
        {
            return bucket;
        }

        return bucket
            .Where(build => !IsSamePlayer(build.PlayerId, excludePlayerId))
            .ToList();
    }

    private static bool IsSamePlayer(string playerId, string otherPlayerId)
    {
        return string.Equals(playerId, otherPlayerId, StringComparison.Ordinal)
               || string.Equals(playerId, $"mirror:{otherPlayerId}", StringComparison.Ordinal)
               || string.Equals($"mirror:{playerId}", otherPlayerId, StringComparison.Ordinal);
    }

    private static BuildSnapshot Pick(IReadOnlyList<BuildSnapshot> candidates) =>
        candidates[Random.Shared.Next(candidates.Count)];

    private static BuildSnapshot PickRated(IReadOnlyList<BuildSnapshot> candidates, int attackerRating)
    {
        if (attackerRating <= 0 || candidates.Count == 1)
        {
            return Pick(candidates);
        }

        var target = ArenaRank.EffectiveSnapshotRating(attackerRating);
        for (var window = 100; window <= 500; window += 50)
        {
            var inWindow = candidates
                .Where(build => Math.Abs(ArenaRank.EffectiveSnapshotRating(build.Rating) - target) <= window)
                .ToList();
            if (inWindow.Count > 0)
            {
                return Pick(inWindow);
            }
        }

        return Pick(candidates);
    }

    public static BuildSnapshot MirrorOf(BuildSnapshot snapshot)
    {
        var playerId = snapshot.PlayerId;
        if (playerId.StartsWith("mirror:", StringComparison.Ordinal))
        {
            return snapshot;
        }

        return snapshot with { PlayerId = $"mirror:{playerId}" };
    }

    public static int ResolveRound(BuildSnapshot snapshot)
    {
        if (snapshot.Round > 0)
        {
            return snapshot.Round;
        }

        var buildId = snapshot.BuildId;
        if (buildId.StartsWith("arena-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(buildId.AsSpan("arena-".Length), out var fromId)
            && fromId > 0)
        {
            return fromId;
        }

        return 1;
    }

    private List<BuildSnapshot> GetRoundList(int round)
    {
        if (!_rounds.TryGetValue(round, out var list))
        {
            list = [];
            _rounds[round] = list;
        }

        return list;
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
        {
            return;
        }

        var json = File.ReadAllText(_path);
        lock (_gate)
        {
            if (TryLoadRoundState(json) || TryLoadLegacyFlatList(json))
            {
                PersistUnlocked();
            }
        }
    }

    private bool TryLoadRoundState(string json)
    {
        try
        {
            var state = JsonSerializer.Deserialize(json, NetCodeJsonContext.Default.BuildPoolState);
            if (state?.Rounds == null || state.Rounds.Count == 0)
            {
                return false;
            }

            foreach (var (key, builds) in state.Rounds)
            {
                if (!int.TryParse(key, out var round) || builds == null)
                {
                    continue;
                }

                foreach (var snapshot in builds)
                {
                    if (string.IsNullOrWhiteSpace(snapshot.PlayerId))
                    {
                        continue;
                    }

                    var stored = snapshot with { Round = snapshot.Round > 0 ? snapshot.Round : round };
                    GetRoundList(stored.Round).Add(stored);
                }
            }

            return _rounds.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool TryLoadLegacyFlatList(string json)
    {
        try
        {
            var snapshots = JsonSerializer.Deserialize(json, NetCodeJsonContext.Default.ListBuildSnapshot);
            if (snapshots == null || snapshots.Count == 0)
            {
                return false;
            }

            foreach (var snapshot in snapshots)
            {
                if (string.IsNullOrWhiteSpace(snapshot.PlayerId))
                {
                    continue;
                }

                var stored = snapshot with { Round = ResolveRound(snapshot) };
                GetRoundList(stored.Round).Add(stored);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void PersistUnlocked()
    {
        if (string.IsNullOrEmpty(_path))
        {
            return;
        }

        var state = new BuildPoolState
        {
            Rounds = _rounds
                .OrderBy(pair => pair.Key)
                .ToDictionary(
                    pair => pair.Key.ToString(),
                    pair => pair.Value.ToList())
        };
        var json = JsonSerializer.Serialize(state, NetCodeJsonContext.Default.BuildPoolState);
        File.WriteAllText(_path, json);
    }
}
