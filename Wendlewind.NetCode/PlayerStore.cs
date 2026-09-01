using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Arena;
using Wendlewind.Sim.Combat;

namespace Wendlewind.NetCode;

public sealed class PlayerStore
{
    private readonly string _playersDir;
    private readonly object _gate = new();

    public PlayerStore(string dataDirectory)
    {
        _playersDir = Path.Combine(dataDirectory, "players");
        Directory.CreateDirectory(_playersDir);
    }

    public PlayerProfileRecord GetOrCreateProfile(string? playerId, string? displayName = null, string? username = null)
    {
        lock (_gate)
        {
            return GetOrCreateProfileUnlocked(playerId, displayName, username);
        }
    }

    public PlayerProfileRecord? GetProfile(string playerId)
    {
        lock (_gate)
        {
            return TryRead(ProfilePath(playerId), NetCodeJsonContext.Default.PlayerProfileRecord);
        }
    }

    public PlayerProfileRecord UpdateProfile(string playerId, string? displayName, string? username)
    {
        lock (_gate)
        {
            var existing = GetOrCreateProfileUnlocked(playerId);
            var updated = existing with
            {
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? existing.DisplayName : displayName,
                Username = username ?? existing.Username,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            Write(ProfilePath(playerId), updated, NetCodeJsonContext.Default.PlayerProfileRecord);
            return updated;
        }
    }

    public AchievementState GetAchievements(string playerId)
    {
        lock (_gate)
        {
            return TryRead(AchievementsPath(playerId), NetCodeJsonContext.Default.AchievementState)
                   ?? new AchievementState();
        }
    }

    public void SaveAchievements(string playerId, AchievementState state)
    {
        lock (_gate)
        {
            GetOrCreateProfileUnlocked(playerId);
            Write(AchievementsPath(playerId), state, NetCodeJsonContext.Default.AchievementState);
        }
    }

    public ArenaProgressRecord? GetCurrentArena(string playerId)
    {
        lock (_gate)
        {
            return ReadCurrentUnlocked(playerId);
        }
    }

    public ArenaProgressRecord SaveCurrentArena(ArenaProgressRecord progress)
    {
        lock (_gate)
        {
            GetOrCreateProfileUnlocked(progress.PlayerId, progress.PlayerName);
            var stored = progress with { UpdatedAt = DateTimeOffset.UtcNow };
            Write(CurrentArenaPath(progress.PlayerId), stored, NetCodeJsonContext.Default.ArenaProgressRecord);
            UpsertRunFromProgressUnlocked(stored);
            return stored;
        }
    }

    public ArenaProgressRecord StartArena(string playerId, string? playerName, int runSeed = 0)
    {
        lock (_gate)
        {
            return StartArenaUnlocked(playerId, playerName, runSeed);
        }
    }

    public ArenaRunRecord? FinishCurrent(string playerId, bool? victory = null)
    {
        lock (_gate)
        {
            return FinishCurrentUnlocked(playerId, abandoned: false, victory);
        }
    }

    public ArenaFightRecord AppendFight(
        string playerId,
        ArenaFightRecord fight,
        IReadOnlyList<CombatLogEvent>? log = null)
    {
        lock (_gate)
        {
            var current = ReadCurrentUnlocked(playerId) ?? StartArenaUnlocked(playerId, fight.Attacker.PawnName, fight.Attacker.Seed);
            var run = ReadRunUnlocked(playerId, current.RunId)
                      ?? new ArenaRunRecord
                      {
                          RunId = current.RunId,
                          PlayerId = playerId,
                          PlayerName = current.PlayerName,
                          RunSeed = current.RunSeed,
                          StartedAt = current.StartedAt
                      };

            var stored = fight with { FoughtAt = fight.FoughtAt == default ? DateTimeOffset.UtcNow : fight.FoughtAt };
            run.Fights.Add(stored);
            WriteRunUnlocked(playerId, current.RunId, run);
            if (log != null)
            {
                WriteCombatLogUnlocked(playerId, current.RunId, stored.MatchId, log);
            }

            return stored;
        }
    }

    public ArenaRunRecord? GetRun(string playerId, string runId)
    {
        lock (_gate)
        {
            return ReadRunUnlocked(playerId, runId);
        }
    }

    public List<ArenaRunRecord> ListRuns(string playerId)
    {
        lock (_gate)
        {
            return ListRunsUnlocked(playerId);
        }
    }

    public List<ArenaRunRecord> ListAllRuns()
    {
        lock (_gate)
        {
            return ListAllRunsUnlocked();
        }
    }

    public List<AdminPlayerRow> ListPlayers()
    {
        lock (_gate)
        {
            return ListPlayerIdsUnlocked()
                .Select(ToPlayerRowUnlocked)
                .OrderByDescending(player => player.LastPlayedAt ?? player.UpdatedAt)
                .ToList();
        }
    }

    public AdminPlayerDetail? GetPlayerDetail(string playerId)
    {
        lock (_gate)
        {
            if (!PlayerExistsUnlocked(playerId))
            {
                return null;
            }

            var current = ReadCurrentUnlocked(playerId);
            var runs = ListRunsUnlocked(playerId);
            return new AdminPlayerDetail
            {
                Player = ToPlayerRowUnlocked(playerId, current, runs),
                Achievements = TryRead(AchievementsPath(playerId), NetCodeJsonContext.Default.AchievementState)
                               ?? new AchievementState(),
                CurrentArena = current,
                Runs = runs.Select(run => ToRunRow(run, current?.RunId)).ToList()
            };
        }
    }

    public List<AdminRunRow> ListAllRunRows()
    {
        lock (_gate)
        {
            var active = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var playerId in ListPlayerIdsUnlocked())
            {
                var current = ReadCurrentUnlocked(playerId);
                if (current != null)
                {
                    active[playerId] = current.RunId;
                }
            }

            return ListAllRunsUnlocked()
                .Select(run =>
                {
                    active.TryGetValue(run.PlayerId, out var activeRunId);
                    return ToRunRow(run, activeRunId);
                })
                .ToList();
        }
    }

    public AdminOverview SummarizeAdmin(int poolBuilds, IReadOnlyList<AdminPoolRound> poolByRound, FightAnalyticsSummary fightSummary)
    {
        lock (_gate)
        {
            var players = ListPlayerIdsUnlocked()
                .Select(ToPlayerRowUnlocked)
                .OrderByDescending(player => player.LastPlayedAt ?? player.UpdatedAt)
                .ToList();
            var runs = ListAllRunsUnlocked();
            var finished = runs.Where(run => run.FinishedAt != null).ToList();
            return new AdminOverview
            {
                Players = players.Count,
                ActiveArenas = players.Count(player => player.HasActiveArena),
                Runs = runs.Count,
                FinishedRuns = finished.Count,
                Victories = finished.Count(run => run.Victory == true),
                Defeats = finished.Count(run => run.Victory == false),
                Abandoned = finished.Count(run => run.Victory == null),
                Fights = runs.Sum(run => run.Fights.Count),
                PoolBuilds = poolBuilds,
                PoolByRound = poolByRound.ToList(),
                FightSummary = fightSummary,
                ActivePlayers = players.Where(player => player.HasActiveArena).ToList()
            };
        }
    }

    public CombatLogRecord? GetCombatLog(string playerId, string runId, string matchId)
    {
        lock (_gate)
        {
            return FindCombatLogUnlocked(playerId, runId, matchId);
        }
    }

    public CombatLogRecord? FindCombatLog(string matchId)
    {
        lock (_gate)
        {
            foreach (var run in ListAllRunsUnlocked())
            {
                var log = FindCombatLogUnlocked(run.PlayerId, run.RunId, matchId);
                if (log != null)
                {
                    return log;
                }
            }

            return null;
        }
    }

    public bool TryUpdateFight(
        string playerId,
        string runId,
        string matchId,
        FightAnalytics analytics,
        IReadOnlyList<CombatLogEvent> log)
    {
        lock (_gate)
        {
            var run = ReadRunUnlocked(playerId, runId);
            if (run == null)
            {
                return false;
            }

            var index = run.Fights.FindIndex(fight => fight.MatchId == matchId);
            if (index < 0)
            {
                return false;
            }

            run.Fights[index] = run.Fights[index] with { Analytics = analytics };
            WriteRunUnlocked(playerId, runId, run);
            WriteCombatLogUnlocked(playerId, runId, matchId, log);
            return true;
        }
    }

    private PlayerProfileRecord GetOrCreateProfileUnlocked(string? playerId, string? displayName = null, string? username = null)
    {
        var id = string.IsNullOrWhiteSpace(playerId) ? Guid.NewGuid().ToString("N") : playerId.Trim();
        EnsurePlayerDir(id);
        var path = ProfilePath(id);
        var existing = TryRead(path, NetCodeJsonContext.Default.PlayerProfileRecord);
        if (existing != null)
        {
            if (displayName == null && username == null)
            {
                return existing;
            }

            var updated = existing with
            {
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? existing.DisplayName : displayName,
                Username = username ?? existing.Username,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            Write(path, updated, NetCodeJsonContext.Default.PlayerProfileRecord);
            return updated;
        }

        var created = new PlayerProfileRecord
        {
            PlayerId = id,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Bilbert" : displayName,
            Username = username ?? "",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        Write(path, created, NetCodeJsonContext.Default.PlayerProfileRecord);
        return created;
    }

    private ArenaProgressRecord? ReadCurrentUnlocked(string playerId)
    {
        return TryRead(CurrentArenaPath(playerId), NetCodeJsonContext.Default.ArenaProgressRecord);
    }

    private ArenaProgressRecord StartArenaUnlocked(string playerId, string? playerName, int runSeed)
    {
        var profile = GetOrCreateProfileUnlocked(playerId, playerName);
        var current = ReadCurrentUnlocked(playerId);
        if (runSeed <= 0)
        {
            runSeed = current is { RunSeed: > 0 } ? current.RunSeed : Random.Shared.Next();
        }

        FinishCurrentUnlocked(playerId, abandoned: true);
        var runId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var name = string.IsNullOrWhiteSpace(playerName) ? profile.DisplayName : playerName;
        var progress = new ArenaProgressRecord
        {
            RunId = runId,
            PlayerId = playerId,
            PlayerName = name,
            RunSeed = runSeed,
            Gold = ArenaRun.StartingGold,
            Phase = "GeneralStore",
            CurrentMerchantMoniker = "GeneralStore",
            StartedAt = now,
            UpdatedAt = now
        };
        var run = new ArenaRunRecord
        {
            RunId = runId,
            PlayerId = playerId,
            PlayerName = name,
            RunSeed = runSeed,
            StartedAt = now
        };
        WriteRunUnlocked(playerId, runId, run);
        Write(CurrentArenaPath(playerId), progress, NetCodeJsonContext.Default.ArenaProgressRecord);
        return progress;
    }

    private void UpsertRunFromProgressUnlocked(ArenaProgressRecord progress)
    {
        var run = ReadRunUnlocked(progress.PlayerId, progress.RunId)
                  ?? new ArenaRunRecord
                  {
                      RunId = progress.RunId,
                      PlayerId = progress.PlayerId,
                      PlayerName = progress.PlayerName,
                      RunSeed = progress.RunSeed,
                      StartedAt = progress.StartedAt
                  };

        var updated = run with
        {
            PlayerName = progress.PlayerName,
            RunSeed = progress.RunSeed,
            Wins = progress.Wins,
            Losses = progress.Losses,
            FinalGold = progress.Gold
        };
        WriteRunUnlocked(progress.PlayerId, progress.RunId, updated);
    }

    private ArenaRunRecord? FinishCurrentUnlocked(string playerId, bool abandoned, bool? victory = null)
    {
        var current = ReadCurrentUnlocked(playerId);
        if (current == null)
        {
            return null;
        }

        var run = ReadRunUnlocked(playerId, current.RunId)
                  ?? new ArenaRunRecord
                  {
                      RunId = current.RunId,
                      PlayerId = playerId,
                      PlayerName = current.PlayerName,
                      RunSeed = current.RunSeed,
                      StartedAt = current.StartedAt
                  };

        var won = victory ?? (current.Wins >= 10 ? true : current.Losses >= 5 ? false : (bool?)null);
        var finished = run with
        {
            FinishedAt = DateTimeOffset.UtcNow,
            Victory = abandoned && won == null ? null : won,
            Wins = current.Wins,
            Losses = current.Losses,
            FinalGold = current.Gold,
            Fights = run.Fights
        };
        WriteRunUnlocked(playerId, current.RunId, finished);
        File.Delete(CurrentArenaPath(playerId));
        return finished;
    }

    private List<ArenaRunRecord> ListRunsUnlocked(string playerId)
    {
        var dir = RunsDir(playerId);
        if (!Directory.Exists(dir))
        {
            return [];
        }

        return DiscoverRunIds(dir)
            .Select(runId => ReadRunUnlocked(playerId, runId))
            .Where(run => run != null)
            .Cast<ArenaRunRecord>()
            .OrderByDescending(run => run.StartedAt)
            .ToList();
    }

    private List<ArenaRunRecord> ListAllRunsUnlocked()
    {
        return ListPlayerIdsUnlocked()
            .SelectMany(ListRunsUnlocked)
            .OrderByDescending(run => run.StartedAt)
            .ToList();
    }

    private IEnumerable<string> ListPlayerIdsUnlocked()
    {
        if (!Directory.Exists(_playersDir))
        {
            return [];
        }

        return Directory.GetDirectories(_playersDir)
            .Select(Path.GetFileName)
            .Where(id => IsSafeId(id!))
            .Cast<string>();
    }

    private bool PlayerExistsUnlocked(string playerId)
    {
        return IsSafeId(playerId) && Directory.Exists(PlayerDir(playerId));
    }

    private AdminPlayerRow ToPlayerRowUnlocked(string playerId)
    {
        return ToPlayerRowUnlocked(playerId, ReadCurrentUnlocked(playerId), ListRunsUnlocked(playerId));
    }

    private AdminPlayerRow ToPlayerRowUnlocked(
        string playerId,
        ArenaProgressRecord? current,
        IReadOnlyList<ArenaRunRecord> runs)
    {
        var profile = TryRead(ProfilePath(playerId), NetCodeJsonContext.Default.PlayerProfileRecord);
        var lastPlayed = profile?.UpdatedAt ?? default;
        if (current != null && current.UpdatedAt > lastPlayed)
        {
            lastPlayed = current.UpdatedAt;
        }

        foreach (var run in runs)
        {
            var stamp = run.FinishedAt ?? run.StartedAt;
            if (stamp > lastPlayed)
            {
                lastPlayed = stamp;
            }
        }

        return new AdminPlayerRow
        {
            PlayerId = profile?.PlayerId ?? playerId,
            DisplayName = profile?.DisplayName ?? playerId,
            Username = profile?.Username ?? "",
            CreatedAt = profile?.CreatedAt ?? default,
            UpdatedAt = profile?.UpdatedAt ?? default,
            RunCount = runs.Count,
            FightCount = runs.Sum(run => run.Fights.Count),
            TotalWins = runs.Sum(run => run.Wins),
            TotalLosses = runs.Sum(run => run.Losses),
            Victories = runs.Count(run => run.Victory == true),
            HasActiveArena = current != null,
            ActivePhase = current?.Phase,
            ActiveWins = current?.Wins,
            ActiveLosses = current?.Losses,
            ActiveGold = current?.Gold,
            LastPlayedAt = lastPlayed == default ? null : lastPlayed
        };
    }

    private static AdminRunRow ToRunRow(ArenaRunRecord run, string? activeRunId)
    {
        return new AdminRunRow
        {
            RunId = run.RunId,
            PlayerId = run.PlayerId,
            PlayerName = run.PlayerName,
            RunSeed = run.RunSeed,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            Victory = run.Victory,
            Wins = run.Wins,
            Losses = run.Losses,
            FinalGold = run.FinalGold,
            FightCount = run.Fights.Count,
            IsActive = activeRunId != null && string.Equals(run.RunId, activeRunId, StringComparison.Ordinal)
        };
    }

    private ArenaRunRecord? ReadRunUnlocked(string playerId, string runId)
    {
        MigrateRunLayoutUnlocked(playerId, runId);
        return TryRead(RunPath(playerId, runId), NetCodeJsonContext.Default.ArenaRunRecord);
    }

    private void WriteRunUnlocked(string playerId, string runId, ArenaRunRecord run)
    {
        Write(RunPath(playerId, runId), run, NetCodeJsonContext.Default.ArenaRunRecord);
        if (!File.Exists(CombatEventsPath(playerId, runId)))
        {
            Write(CombatEventsPath(playerId, runId), new CombatEventsFile(), NetCodeJsonContext.Default.CombatEventsFile);
        }
    }

    private void WriteCombatLogUnlocked(
        string playerId,
        string runId,
        string matchId,
        IReadOnlyList<CombatLogEvent> log)
    {
        var archive = TryRead(CombatEventsPath(playerId, runId), NetCodeJsonContext.Default.CombatEventsFile)
                      ?? new CombatEventsFile();
        var record = new CombatLogRecord
        {
            MatchId = matchId,
            Events = log as CombatLogEvent[] ?? log.ToArray()
        };
        var index = archive.Fights.FindIndex(fight => fight.MatchId == matchId);
        if (index >= 0)
        {
            archive.Fights[index] = record;
        }
        else
        {
            archive.Fights.Add(record);
        }

        Write(CombatEventsPath(playerId, runId), archive, NetCodeJsonContext.Default.CombatEventsFile);
    }

    private CombatLogRecord? FindCombatLogUnlocked(string playerId, string runId, string matchId)
    {
        MigrateRunLayoutUnlocked(playerId, runId);
        var archive = TryRead(CombatEventsPath(playerId, runId), NetCodeJsonContext.Default.CombatEventsFile);
        return archive?.Fights.FirstOrDefault(fight => fight.MatchId == matchId);
    }

    private void MigrateRunLayoutUnlocked(string playerId, string runId)
    {
        if (!IsSafeId(runId))
        {
            return;
        }

        var matchPath = RunPath(playerId, runId);
        var legacyRunPath = LegacyRunPath(playerId, runId);
        if (!File.Exists(matchPath) && File.Exists(legacyRunPath))
        {
            Directory.CreateDirectory(RunDir(playerId, runId));
            File.Move(legacyRunPath, matchPath);
        }

        var eventsPath = CombatEventsPath(playerId, runId);
        var legacyLogsDir = LegacyLogsDir(playerId, runId);
        if (!File.Exists(eventsPath) && Directory.Exists(legacyLogsDir))
        {
            var archive = new CombatEventsFile();
            foreach (var file in Directory.GetFiles(legacyLogsDir, "*.json"))
            {
                var record = TryRead(file, NetCodeJsonContext.Default.CombatLogRecord);
                if (record != null)
                {
                    archive.Fights.Add(record);
                }
            }

            Write(eventsPath, archive, NetCodeJsonContext.Default.CombatEventsFile);
            Directory.Delete(legacyLogsDir, true);
        }
        else if (File.Exists(matchPath) && !File.Exists(eventsPath))
        {
            Write(eventsPath, new CombatEventsFile(), NetCodeJsonContext.Default.CombatEventsFile);
        }
    }

    private static IEnumerable<string> DiscoverRunIds(string runsDir)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in Directory.GetDirectories(runsDir))
        {
            var id = Path.GetFileName(directory);
            if (IsSafeId(id))
            {
                ids.Add(id);
            }
        }

        foreach (var file in Directory.GetFiles(runsDir, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(file);
            if (IsSafeId(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private void EnsurePlayerDir(string playerId)
    {
        Directory.CreateDirectory(PlayerDir(playerId));
        Directory.CreateDirectory(RunsDir(playerId));
    }

    private string PlayerDir(string playerId) => Path.Combine(_playersDir, Sanitize(playerId));

    private string ProfilePath(string playerId) => Path.Combine(PlayerDir(playerId), "profile.json");

    private string AchievementsPath(string playerId) => Path.Combine(PlayerDir(playerId), "achievements.json");

    private string CurrentArenaPath(string playerId) => Path.Combine(PlayerDir(playerId), "arena-current.json");

    private string RunsDir(string playerId) => Path.Combine(PlayerDir(playerId), "arena-runs");

    private string RunDir(string playerId, string runId) =>
        Path.Combine(RunsDir(playerId), Sanitize(runId));

    private string RunPath(string playerId, string runId) =>
        Path.Combine(RunDir(playerId, runId), "match.json");

    private string CombatEventsPath(string playerId, string runId) =>
        Path.Combine(RunDir(playerId, runId), "combat-events.json");

    private string LegacyRunPath(string playerId, string runId) =>
        Path.Combine(RunsDir(playerId), $"{Sanitize(runId)}.json");

    private string LegacyLogsDir(string playerId, string runId) =>
        Path.Combine(RunDir(playerId, runId), "logs");

    private static bool IsSafeId(string id) =>
        !string.IsNullOrWhiteSpace(id) && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private static string Sanitize(string id)
    {
        if (!IsSafeId(id))
        {
            throw new ArgumentException("Invalid id for file storage.", nameof(id));
        }

        return id;
    }

    private static T? TryRead<T>(string path, JsonTypeInfo<T> typeInfo)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, typeInfo);
    }

    private static void Write<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, typeInfo);
        File.WriteAllText(path, json);
    }
}
