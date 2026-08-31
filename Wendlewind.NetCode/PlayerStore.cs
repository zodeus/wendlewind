using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim.Arena;

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

    public ArenaFightRecord AppendFight(string playerId, ArenaFightRecord fight)
    {
        lock (_gate)
        {
            var current = ReadCurrentUnlocked(playerId) ?? StartArenaUnlocked(playerId, fight.Attacker.PawnName, fight.Attacker.Seed);
            var path = RunPath(playerId, current.RunId);
            var run = TryRead(path, NetCodeJsonContext.Default.ArenaRunRecord)
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
            Write(path, run, NetCodeJsonContext.Default.ArenaRunRecord);
            return stored;
        }
    }

    public ArenaRunRecord? GetRun(string playerId, string runId)
    {
        lock (_gate)
        {
            return TryRead(RunPath(playerId, runId), NetCodeJsonContext.Default.ArenaRunRecord);
        }
    }

    public List<ArenaRunRecord> ListRuns(string playerId)
    {
        lock (_gate)
        {
            var dir = RunsDir(playerId);
            if (!Directory.Exists(dir))
            {
                return [];
            }

            return Directory.GetFiles(dir, "*.json")
                .Select(path => TryRead(path, NetCodeJsonContext.Default.ArenaRunRecord))
                .Where(run => run != null)
                .Cast<ArenaRunRecord>()
                .OrderByDescending(run => run.StartedAt)
                .ToList();
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
        Write(RunPath(playerId, runId), run, NetCodeJsonContext.Default.ArenaRunRecord);
        Write(CurrentArenaPath(playerId), progress, NetCodeJsonContext.Default.ArenaProgressRecord);
        return progress;
    }

    private void UpsertRunFromProgressUnlocked(ArenaProgressRecord progress)
    {
        var path = RunPath(progress.PlayerId, progress.RunId);
        var run = TryRead(path, NetCodeJsonContext.Default.ArenaRunRecord)
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
        Write(path, updated, NetCodeJsonContext.Default.ArenaRunRecord);
    }

    private ArenaRunRecord? FinishCurrentUnlocked(string playerId, bool abandoned, bool? victory = null)
    {
        var current = ReadCurrentUnlocked(playerId);
        if (current == null)
        {
            return null;
        }

        var run = TryRead(RunPath(playerId, current.RunId), NetCodeJsonContext.Default.ArenaRunRecord)
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
        Write(RunPath(playerId, current.RunId), finished, NetCodeJsonContext.Default.ArenaRunRecord);
        File.Delete(CurrentArenaPath(playerId));
        return finished;
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

    private string RunPath(string playerId, string runId) =>
        Path.Combine(RunsDir(playerId), $"{Sanitize(runId)}.json");

    private static string Sanitize(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
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
