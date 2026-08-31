namespace Wendlewind.NetCode.Contracts;

public sealed record PlayerProfileRecord
{
    public required string PlayerId { get; init; }
    public string DisplayName { get; init; } = "Bilbert";
    public string Username { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record CreatePlayerRequest
{
    public string? PlayerId { get; init; }
    public string? DisplayName { get; init; }
    public string? Username { get; init; }
}

public sealed record AchievementRecord
{
    public required string Moniker { get; init; }
    public float CurrentValue { get; init; }
    public bool IsUnlocked { get; init; }
    public DateTimeOffset? UnlockedAt { get; init; }
    public bool IsAcknowledged { get; init; }
}

public sealed record AchievementState
{
    public List<AchievementRecord> Achievements { get; init; } = [];
}

public sealed record ArenaProgressRecord
{
    public required string RunId { get; init; }
    public required string PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int RunSeed { get; init; }
    public int Gold { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public string Phase { get; init; } = "GeneralStore";
    public string? CurrentMerchantMoniker { get; init; }
    public List<string> FoughtPlayerIds { get; init; } = [];
    public string? LastOpponentPlayerId { get; init; }
    public bool LastFightWon { get; init; }
    public int LastGoldDelta { get; init; }
    public BuildSnapshot? Loadout { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record ArenaFightRecord
{
    public required string MatchId { get; init; }
    public int Round { get; init; }
    public required BuildSnapshot Attacker { get; init; }
    public required BuildSnapshot Defender { get; init; }
    public int EncounterSeed { get; init; }
    public required string WinnerPlayerId { get; init; }
    public int Ticks { get; init; }
    public string? CauseOfDeath { get; init; }
    public DateTimeOffset FoughtAt { get; init; }
}

public sealed record ArenaRunRecord
{
    public required string RunId { get; init; }
    public required string PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int RunSeed { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public bool? Victory { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int FinalGold { get; init; }
    public List<ArenaFightRecord> Fights { get; init; } = [];
}

public sealed record StartArenaRequest
{
    public int? RunSeed { get; init; }
    public string? PlayerName { get; init; }
}
